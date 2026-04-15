namespace Grimoire.Infrastructure.Export.Epub;

using System.Text;
using System.Web;
using Application.Dto.Book;
using Application.Export;
using Application.Extensions;
using Application.Service.Strategy;
using Common;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Microsoft.Extensions.Logging;

public partial class EpubSectionRenderer(
	ITemplateEngine templateEngine,
	ILogger<EpubSectionRenderer> logger) : ISectionRenderer {
	public ExportFormat Format => ExportFormat.Epub;

	public string RenderSegments(IEnumerable<SegmentModel> segments, List<FootnoteSegmentModel>? footnotes = null,
		IReadOnlyDictionary<string, string>? assetMap = null) {
		var segmentList = segments.ToList();
		if (segmentList.Count == 0) {
			return string.Empty;
		}

		var footnoteMap = BuildFootnoteMap(segmentList);
		var sb = new StringBuilder();

		foreach (var segment in segmentList) {
			switch (segment) {
				case TextSegmentModel ts:
					sb.Append($"<p>{RenderTextRuns(ts.Runs, footnoteMap)}</p>");
					break;
				case ImageSegmentModel ism:
					var url = assetMap?.TryGetValue(ism.AssetKey, out var fileName) == true
						? $"images/{fileName}"
						: ism.AssetKey;
					var encodedCaption = HttpUtility.HtmlEncode(ism.Caption ?? string.Empty);
					sb.Append(
						$"<figure><img src=\"{HttpUtility.HtmlEncode(url)}\" alt=\"{encodedCaption}\"/><figcaption>{encodedCaption}</figcaption></figure>");
					break;
				case DividerSegmentModel ds:
					if (ds.Style != default!) {
						sb.Append($"<p>{ds}</p");
					}

					sb.Append("<hr class=\"divider\" aria-hidden=\"true\" />");
					break;
				case FootnoteSegmentModel fs:
					sb.Append($"<aside class=\"footnote-inline\">{RenderFootnoteContent(fs)}</aside>");
					break;
				default:
					break;
			}
		}

		if (footnotes is { Count: > 0 }) {
			sb.Append(RenderFootnotesAtEnd(footnotes, footnoteMap));
		}

		return sb.ToString();
	}

	public string RenderDescription(IEnumerable<TextSegmentModel> segments,
		IReadOnlyDictionary<string, string>? assetMap = null) {
		var segmentList = segments.ToList();
		if (segmentList.Count == 0) {
			return string.Empty;
		}

		var sb = new StringBuilder();
		foreach (var segment in segmentList) {
			sb.Append($"<p>{RenderTextRuns(segment.Runs)}</p>");
		}

		return sb.ToString();
	}

	private static Dictionary<string, int> BuildFootnoteMap(List<SegmentModel> segments) {
		var footnoteMap = new Dictionary<string, int>();
		var footnoteCounter = 1;

		foreach (var segment in segments) {
			if (segment is not TextSegmentModel textSeg) {
				continue;
			}

			foreach (var run in textSeg.Runs) {
				if (!string.IsNullOrEmpty(run.FootnoteId) && !footnoteMap.ContainsKey(run.FootnoteId)) {
					footnoteMap[run.FootnoteId] = footnoteCounter++;
				}
			}
		}

		return footnoteMap;
	}

	private static string RenderTextRuns(IEnumerable<TextRun> runs, Dictionary<string, int>? footnoteMap = null) {
		var sb = new StringBuilder();
		foreach (var run in runs) {
			var text = HttpUtility.HtmlEncode(run.Text);
			text = FormatText(text, run.IsBold, run.IsItalic);

			if (!string.IsNullOrEmpty(run.FootnoteId) && footnoteMap != null &&
				footnoteMap.TryGetValue(run.FootnoteId, out var footnoteNumber)) {
				text +=
					$"<a class=\"footnote-ref\" epub:type=\"noteref\" href=\"#{run.FootnoteId}\">[{footnoteNumber}]</a>";
			}

			sb.Append(text);
		}

		return sb.ToString();
	}

	private static string FormatText(string text, bool isBold, bool isItalic) =>
		(isBold, isItalic) switch {
			(true, true) => $"<strong><em>{text}</em></strong>",
			(true, false) => $"<strong>{text}</strong>",
			(false, true) => $"<em>{text}</em>",
			_ => text
		};

	private static string RenderFootnoteContent(FootnoteSegmentModel footnote) {
		var sb = new StringBuilder();
		foreach (var textSegment in footnote.Segments) {
			sb.Append($"<p>{RenderTextRuns(textSegment.Runs)}</p>");
		}

		return sb.ToString();
	}

	private static string RenderFootnotesAtEnd(List<FootnoteSegmentModel> footnotes,
		Dictionary<string, int> footnoteMap) {
		if (footnotes == null || footnotes.Count == 0) {
			return string.Empty;
		}

		var sb = new StringBuilder();
		sb.AppendLine("<aside class=\"footnotes\" epub:type=\"footnotes\" role=\"doc-footnote\">");

		foreach (var footnote in footnotes) {
			var footnoteIdStr = footnote.Id.ToString();
			if (!footnoteMap.TryGetValue(footnoteIdStr, out var footnoteNumber)) {
				continue;
			}

			sb.AppendLine($"<div id=\"{footnoteIdStr}\" epub:type=\"footnote\">");
			sb.AppendLine($"<p>[{footnoteNumber}] ");

			var contentSb = new StringBuilder();
			foreach (var textSeg in footnote.Segments) {
				contentSb.Append(RenderTextRuns(textSeg.Runs));
			}

			sb.Append(contentSb);

			sb.AppendLine("</p>");
			sb.AppendLine("</div>");
		}

		sb.AppendLine("</aside>");
		return sb.ToString();
	}

	public IReadOnlyList<NavEntry> RenderSection(
		BookExportContext context,
		ExportSectionDto section,
		IPackageBuilder builder) => section.Type switch {
			BookSection.Intro or BookSection.IntroPage => RenderIntro(context, section, builder),
			BookSection.Toc or BookSection.TableOfContents => RenderToc(builder),
			BookSection.Description => RenderDescription(context, section, builder),
			BookSection.Content or BookSection.Chapters => RenderContent(context, builder),
			BookSection.Unknown => throw new NotImplementedException(),
			_ => []
		};

	private IReadOnlyList<NavEntry> RenderIntro(BookExportContext context, ExportSectionDto section,
		IPackageBuilder builder) {
		var renderedDescription = context.Series.Metadata?.Description != null
			? RenderDescription(context.Series.Metadata.Description, context.AssetFileMap)
			: string.Empty;

		var html = templateEngine.Render("epub_intro",
			new {
				context.Series.Title,
				Author = context.Series.Metadata?.Authors?.FirstOrDefault(),
				context.Series.Metadata?.Tags,
				RenderedDescription = renderedDescription,
				Section = section,
				CoverLocalPath = ResolveCoverLocalPath(context),
				ImageFileMap = context.AssetFileMap
			});

		builder.AddPage("intro", html, PageRole.Intro);
		return [new NavEntry("intro", EpubConstants.LocalizedText.INTRODUCTION)];
	}

	private static IReadOnlyList<NavEntry> RenderToc(IPackageBuilder builder) {
		// Content is empty because IPackageBuilder generates the nav.xhtml automatically
		builder.AddPage("toc", string.Empty, PageRole.TableOfContents);
		return [new NavEntry("toc", EpubConstants.LocalizedText.TABLE_OF_CONTENTS)];
	}

	private IReadOnlyList<NavEntry> RenderDescription(BookExportContext context, ExportSectionDto section,
		IPackageBuilder builder) {
		var introSection = context.Structure.Sections.FirstOrDefault(s => s.Type == BookSection.IntroPage);

		if (introSection != null && !ExportUtilities.IsSplitDescriptionEnabled(introSection)) {
			LogSkippingDescription();
			return [];
		}

		var html = templateEngine.Render("epub_intro",
			new {
				Title = EpubConstants.LocalizedText.SUMMARY,
				context.Series.Metadata?.Description,
				Section = section,
				ImageFileMap = context.AssetFileMap
			});

		builder.AddPage("description", html, PageRole.Description);
		return [new NavEntry("description", EpubConstants.LocalizedText.SUMMARY)];
	}

	private IReadOnlyList<NavEntry> RenderContent(BookExportContext context, IPackageBuilder builder) {
		var navEntries = new List<NavEntry>();

		foreach (var volume in context.Volumes) {
			var volId = $"vol_{volume.Id:N}";
			var volHtml = templateEngine.Render("epub_volume",
				new {
					volume.Title,
					CoverImagePath =
						volume.Metadata?.CoverImage != null &&
						context.AssetFileMap.TryGetValue(volume.Metadata.CoverImage, out var path)
							? path
							: null,
					volume.Metadata?.PublicationDate,
					volume.Metadata?.Isbn
				});

			var volFileName = builder.AddPage(volId, volHtml, PageRole.VolumeTitle);

			var children = new List<NavEntry>();
			if (context.ChapterMap.TryGetValue(volume.Id, out var chapters)) {
				children.AddRange(from chapter in chapters
								  let chId = chapter.Id.ToString()
								  let renderedContent = chapter.ContentData?.Segments != null
									  ? RenderSegments(chapter.ContentData.Segments, chapter.ContentData.Footnotes,
										  context.AssetFileMap)
									  : string.Empty
								  let chHtml = templateEngine.Render("epub_chapter",
									  new { chapter.Title, RenderedContent = renderedContent })
								  let chFileName = builder.AddPage(chId, chHtml)
								  select new NavEntry(chFileName, chapter.Title));
			}

			navEntries.Add(new NavEntry(volFileName, volume.Title, children));
		}

		return navEntries;
	}

	private static string? ResolveCoverLocalPath(BookExportContext context) {
		if (context.CoverAsset == null) {
			return null;
		}

		var ext = Path.GetExtension(context.CoverAsset.Path).DefaultIfNullOrEmpty(".jpg");
		return $"{EpubConstants.Paths.IMAGES_FOLDER}cover{ext}";
	}

	[LoggerMessage(LogLevel.Information, "Skipping separate description (already in intro)")]
	partial void LogSkippingDescription();
}
