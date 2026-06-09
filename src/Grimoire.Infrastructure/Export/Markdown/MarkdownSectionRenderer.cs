namespace Grimoire.Infrastructure.Export.Markdown;

using System.Text;
using Application.Dto.Book;
using Application.Export;
using Application.Service.Strategy;
using Common;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Microsoft.Extensions.Logging;

public partial class MarkdownSectionRenderer(
	ILogger<MarkdownSectionRenderer> logger) : ISectionRenderer {
	public ExportFormat Format => ExportFormat.Markdown;

	public string RenderSegments(IEnumerable<SegmentModel> segments, List<FootnoteSegmentModel>? footnotes = null, IReadOnlyDictionary<string, string>? assetMap = null) {
		return RenderSegments(segments, footnotes, assetMap, FootnoteStyle.Parentheses, false);
	}

	public string RenderSegments(IEnumerable<SegmentModel> segments, List<FootnoteSegmentModel>? footnotes,
		IReadOnlyDictionary<string, string>? assetMap, FootnoteStyle style, bool enableDropcap) {
		var segmentList = segments.ToList();
		if (segmentList.Count == 0) {
			return string.Empty;
		}

		var footnoteMap = BuildFootnoteMap(footnotes);
		var sb = new StringBuilder();
		var isFirstText = true;

		foreach (var segment in segmentList) {
			var markdown = ConvertSegmentToMarkdown(segment, footnoteMap, style, enableDropcap, ref isFirstText);
			if (!string.IsNullOrWhiteSpace(markdown)) {
				sb.AppendLine(markdown);
				sb.AppendLine();
			}
		}

		if (footnotes is { Count: > 0 }) {
			sb.AppendLine();
			AppendFootnotes(sb, footnotes, footnoteMap, style);
		}

		return sb.ToString();
	}

	public string RenderDescription(IEnumerable<TextSegmentModel> segments, IReadOnlyDictionary<string, string>? assetMap = null) {
		var segmentList = segments.ToList();
		if (segmentList.Count == 0) {
			return string.Empty;
		}

		var sb = new StringBuilder();
		foreach (var segment in segmentList) {
			var markdown = ConvertTextSegmentToMarkdown(segment);
			if (!string.IsNullOrWhiteSpace(markdown)) {
				sb.AppendLine(markdown);
			}
		}

		return sb.ToString();
	}

	public IReadOnlyList<NavEntry> RenderSection(
		BookExportContext context,
		ExportSectionDto section,
		IPackageBuilder builder) => section.Type switch {
			BookSection.Intro or BookSection.IntroPage => RenderIntro(context, section, builder),
			BookSection.Toc or BookSection.TableOfContents => RenderToc(context, builder),
			BookSection.Description => RenderDescriptionSection(context, section, builder),
			BookSection.Content or BookSection.Chapters => RenderContent(context, builder),
			BookSection.Unknown or _ => []
		};

	private static IReadOnlyList<NavEntry> RenderIntro(BookExportContext context, ExportSectionDto section, IPackageBuilder builder) {
		var sb = new StringBuilder();

		sb.AppendLine($"# {context.Series.Title}");
		sb.AppendLine();

		if (context.Series.Metadata?.Authors != null && context.Series.Metadata.Authors.Count > 0) {
			sb.AppendLine($"**{context.Structure.Localization.AuthorLabel}** {string.Join(", ", context.Series.Metadata.Authors)}");
			sb.AppendLine();
		}

		if (context.Series.Metadata?.Tags != null && context.Series.Metadata.Tags.Count > 0) {
			sb.AppendLine($"**Tags:** {string.Join(", ", context.Series.Metadata.Tags)}");
			sb.AppendLine();
		}

		if (context.CoverAsset != null) {
			var ext = Path.GetExtension(context.CoverAsset.Path);
			if (string.IsNullOrEmpty(ext)) {
				ext = ".jpg";
			}

			sb.AppendLine($"![Cover](images/cover{ext})");
			sb.AppendLine();
		}

		builder.AddPage("intro", sb.ToString(), PageRole.Intro);
		return [new NavEntry("intro", context.Structure.Localization.IntroductionLabel)];
	}

	private static IReadOnlyList<NavEntry> RenderToc(BookExportContext context, IPackageBuilder builder) {
		var sb = new StringBuilder();

		sb.AppendLine($"# {context.Structure.Localization.TableOfContentsLabel}");
		sb.AppendLine();

		foreach (var volume in context.Volumes) {
			sb.AppendLine($"## {volume.Title}");
			sb.AppendLine();

			if (context.ChapterMap.TryGetValue(volume.Id, out var chapters)) {
				foreach (var chapter in chapters) {
					sb.AppendLine($"- [{chapter.Title}](#chapter_{chapter.Id:N})");
				}
			}

			sb.AppendLine();
		}

		builder.AddPage("toc", sb.ToString(), PageRole.TableOfContents);
		return [new NavEntry("toc", context.Structure.Localization.TableOfContentsLabel)];
	}

	private IReadOnlyList<NavEntry> RenderDescriptionSection(BookExportContext context, ExportSectionDto section, IPackageBuilder builder) {
		var introSection = context.Structure.Sections.FirstOrDefault(s => s.Type == BookSection.IntroPage);

		if (introSection != null && !IsSplitDescriptionEnabled(introSection)) {
			LogSkippingDescription();
			return [];
		}

		var sb = new StringBuilder();

		sb.AppendLine($"# {context.Structure.Localization.SummaryLabel}");
		sb.AppendLine();

		if (context.Series.Metadata?.Description is { Count: > 0 }) {
			foreach (var segment in context.Series.Metadata.Description) {
				var markdown = ConvertTextSegmentToMarkdown(segment);
				if (!string.IsNullOrWhiteSpace(markdown)) {
					sb.AppendLine(markdown);
				}
			}
			sb.AppendLine();
		}

		builder.AddPage("description", sb.ToString(), PageRole.Description);
		return [new NavEntry("description", context.Structure.Localization.SummaryLabel)];
	}

	private static List<NavEntry> RenderContent(BookExportContext context, IPackageBuilder builder) {
		var navEntries = new List<NavEntry>();

		foreach (var volume in context.Volumes) {
			var volSb = new StringBuilder();

			volSb.AppendLine($"# {volume.Title}");
			volSb.AppendLine();

			if (volume.Metadata?.CoverImage != null && context.AssetFileMap.TryGetValue(volume.Metadata.CoverImage, out var coverPath)) {
				volSb.AppendLine($"![Cover]({coverPath})");
				volSb.AppendLine();
			}

			if (volume.Metadata?.PublicationDate != null) {
				volSb.AppendLine($"**{context.Structure.Localization.PublicationDateLabel}** {volume.Metadata.PublicationDate.Value:yyyy-MM-dd}");
				volSb.AppendLine();
			}

			if (volume.Metadata?.Isbn != null) {
				volSb.AppendLine($"**ISBN:** {volume.Metadata.Isbn}");
				volSb.AppendLine();
			}

			builder.AddPage($"vol_{volume.Id:N}", volSb.ToString(), PageRole.VolumeTitle);

			var children = new List<NavEntry>();
			if (context.ChapterMap.TryGetValue(volume.Id, out var chapters)) {
				foreach (var chapter in chapters) {
					var chSb = new StringBuilder();

					chSb.AppendLine($"## {chapter.Title}");
					chSb.AppendLine();

					if (chapter.ContentData?.Segments != null) {
						var footnoteList = chapter.ContentData.Footnotes;
						var footnoteMap = BuildFootnoteMap(footnoteList);

						var isFirstText = true;
						foreach (var segment in chapter.ContentData.Segments) {
							var markdown = ConvertSegmentToMarkdown(segment, footnoteMap, context.Structure.FootnoteStyle, context.Structure.EnableDropcap, ref isFirstText);
							if (!string.IsNullOrWhiteSpace(markdown)) {
								chSb.AppendLine(markdown);
								chSb.AppendLine();
							}
						}

						if (footnoteList != null && footnoteList.Count > 0) {
							chSb.AppendLine();
							AppendFootnotes(chSb, footnoteList, footnoteMap, context.Structure.FootnoteStyle);
						}
					}

					builder.AddPage($"chapter_{chapter.Id:N}", chSb.ToString(), PageRole.Chapter);
					children.Add(new NavEntry($"chapter_{chapter.Id:N}", chapter.Title));
				}
			}

			navEntries.Add(new NavEntry($"vol_{volume.Id:N}", volume.Title, children));
		}

		return navEntries;
	}

	private static Dictionary<string, int> BuildFootnoteMap(List<FootnoteSegmentModel>? footnotes) {
		var map = new Dictionary<string, int>();
		if (footnotes is null || footnotes.Count == 0) {
			return map;
		}

		for (var i = 0; i < footnotes.Count; i++) {
			map[footnotes[i].Id.ToString()] = i + 1;
		}

		return map;
	}

	private static string FormatFootnoteLabel(int number, FootnoteStyle style) => style switch {
		FootnoteStyle.Parentheses => $"({number})",
		FootnoteStyle.Asterisk => new string('*', number),
		FootnoteStyle.SuperScript => number.ToString(),
		_ => $"[{number}]"
	};

	private static string ConvertSegmentToMarkdown(SegmentModel segment, Dictionary<string, int> footnoteMap, FootnoteStyle style, bool enableDropcap, ref bool isFirstText) =>
		segment switch {
			TextSegmentModel textSegment => ConvertTextSegmentToMarkdown(textSegment, footnoteMap, style, enableDropcap, ref isFirstText),
			ImageSegmentModel imageSegment => ConvertImageSegmentToMarkdown(imageSegment),
			DividerSegmentModel dividerSegment => ConvertDividerSegmentToMarkdown(dividerSegment),
			_ => string.Empty
		};

	private static string ConvertTextSegmentToMarkdown(TextSegmentModel segment, Dictionary<string, int>? footnoteMap = null) {
		var isFirstText = false;
		return ConvertTextSegmentToMarkdown(segment, footnoteMap, FootnoteStyle.Parentheses, false, ref isFirstText);
	}

	private static string ConvertTextSegmentToMarkdown(TextSegmentModel segment, Dictionary<string, int>? footnoteMap, FootnoteStyle style, bool enableDropcap, ref bool isFirstText) {
		if (segment.Runs.Count == 0) {
			return string.Empty;
		}

		var sb = new StringBuilder();

		if (enableDropcap && isFirstText) {
			isFirstText = false;
			var runsList = segment.Runs.ToList();
			var firstTextRunIndex = runsList.FindIndex(r => !string.IsNullOrEmpty(r.Text));
			if (firstTextRunIndex == -1) {
				return ConvertTextRunsToMarkdown(runsList, footnoteMap, style);
			}

			for (int i = 0; i < runsList.Count; i++) {
				var run = runsList[i];
				if (i == firstTextRunIndex) {
					var text = run.Text;
					int letterIdx = 0;
					while (letterIdx < text.Length && !char.IsLetterOrDigit(text[letterIdx])) {
						letterIdx++;
					}

					if (letterIdx < text.Length) {
						var prefix = text.Substring(0, letterIdx);
						var dropcapChar = text[letterIdx];
						var suffix = text.Substring(letterIdx + 1);

						var formattedSuffix = FormatText(suffix, run.IsBold, run.IsItalic);
						var dropcapSpan = $"<span class=\"dropcap\">{dropcapChar}</span>";
						var fullText = prefix + dropcapSpan + formattedSuffix;

						if (!string.IsNullOrEmpty(run.FootnoteId) && footnoteMap != null &&
							footnoteMap.TryGetValue(run.FootnoteId, out var footnoteNumber)) {
							fullText += $"[^{footnoteNumber}]";
						}
						sb.Append(fullText);
					} else {
						sb.Append(ConvertTextRunsToMarkdown(new[] { run }, footnoteMap, style));
					}
				} else {
					sb.Append(ConvertTextRunsToMarkdown(new[] { run }, footnoteMap, style));
				}
			}
			return sb.ToString();
		} else {
			isFirstText = false;
			return ConvertTextRunsToMarkdown(segment.Runs, footnoteMap, style);
		}
	}

	private static string ConvertTextRunsToMarkdown(IEnumerable<TextRun> runs, Dictionary<string, int>? footnoteMap, FootnoteStyle style) {
		var sb = new StringBuilder();
		foreach (var run in runs) {
			var text = run.Text;
			int footnoteIndex = 0;
			var hasFootnote = !string.IsNullOrEmpty(run.FootnoteId) &&
							footnoteMap is not null &&
							footnoteMap.TryGetValue(run.FootnoteId, out footnoteIndex);

			if (string.IsNullOrEmpty(text) && hasFootnote) {
				sb.Append($"[^{footnoteIndex}]");
				continue;
			}

			if (hasFootnote) {
				text += $"[^{footnoteIndex}]";
			}

			text = FormatText(text, run.IsBold, run.IsItalic);
			sb.Append(text);
		}
		return sb.ToString();
	}

	private static string FormatText(string text, bool isBold, bool isItalic) =>
		(isBold, isItalic) switch {
			(true, true) => $"***{text}***",
			(true, false) => $"**{text}**",
			(false, true) => $"*{text}*",
			_ => text
		};

	private static string ConvertImageSegmentToMarkdown(ImageSegmentModel segment) {
		var altText = segment.Caption ?? "Image";
		return $"![{altText}]({segment.AssetKey})";
	}

	private static string ConvertDividerSegmentToMarkdown(DividerSegmentModel segment) => "---";

	private static void AppendFootnotes(StringBuilder sb, List<FootnoteSegmentModel> footnotes, Dictionary<string, int> footnoteMap, FootnoteStyle style) {
		foreach (var footnote in footnotes) {
			if (!footnoteMap.TryGetValue(footnote.Id.ToString(), out var index)) {
				continue;
			}

			var content = ConvertFootnoteContent(footnote.Segments);
			var label = FormatFootnoteLabel(index, style);
			sb.AppendLine($"[^{index}]: {label} {content}");
		}
	}

	private static string ConvertFootnoteContent(List<TextSegmentModel> segments) {
		if (segments.Count == 0) {
			return string.Empty;
		}

		var parts = new List<string>();
		foreach (var segment in segments) {
			var text = ConvertTextSegmentToMarkdown(segment);
			if (!string.IsNullOrWhiteSpace(text)) {
				parts.Add(text);
			}
		}

		return string.Join(" ", parts);
	}

	private static bool IsSplitDescriptionEnabled(ExportSectionDto section) =>
		section.Options?.TryGetValue("SplitDescription", out var value) == true &&
		value is bool splitEnabled && splitEnabled;

	[LoggerMessage(LogLevel.Information, "Skipping separate description (already in intro)")]
	partial void LogSkippingDescription();
}
