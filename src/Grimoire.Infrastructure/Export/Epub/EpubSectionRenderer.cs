namespace Grimoire.Infrastructure.Export.Epub;

using System.Text;
using System.Web;
using Application.Dto.Book;
using Application.Dto.Book.Tree;
using Application.Export;
using Application.Extensions;
using Application.Service.Strategy;
using Common;
using Domain.Common;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Microsoft.Extensions.Logging;

public partial class EpubSectionRenderer(
	ITemplateEngine templateEngine,
	ILogger<EpubSectionRenderer> logger) : ISectionRenderer {
	public ExportFormat Format => ExportFormat.Epub;

	// ── Data structures for consolidated endnotes ──────────────────────────

	private sealed record EndnoteEntry(
		string FootnoteId,
		int Number,
		FootnoteSegmentModel Footnote,
		string ChapterTitle,
		string ChapterFileName);

	// ── ISectionRenderer.RenderSegments (public, unchanged contract) ──────

	public string RenderSegments(IEnumerable<SegmentModel> segments, List<FootnoteSegmentModel>? footnotes = null,
		IReadOnlyDictionary<string, string>? assetMap = null) {
		return RenderSegments(segments, footnotes, assetMap, FootnoteStyle.Parentheses, false);
	}

	public string RenderSegments(IEnumerable<SegmentModel> segments, List<FootnoteSegmentModel>? footnotes,
		IReadOnlyDictionary<string, string>? assetMap, FootnoteStyle style, bool enableDropcap) {
		var segmentList = segments.ToList();
		if (segmentList.Count == 0) {
			return string.Empty;
		}

		var footnoteMap = BuildFootnoteMap(segmentList);
		var sb = new StringBuilder();
		var isFirstText = true;

		foreach (var segment in segmentList) {
			RenderSegmentBody(sb, segment, footnoteMap, assetMap, null, ref isFirstText, enableDropcap, style);
		}

		if (footnotes is { Count: > 0 }) {
			sb.Append(RenderFootnotesAtEnd(footnotes, footnoteMap, style));
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

	// ── Segment rendering (shared between inline and consolidated) ────────

	private static void RenderSegmentBody(StringBuilder sb, SegmentModel segment,
		Dictionary<string, int> footnoteMap, IReadOnlyDictionary<string, string>? assetMap,
		string? endnotesFile, ref bool isFirstText, bool enableDropcap, FootnoteStyle style) {
		switch (segment) {
			case TextSegmentModel ts:
				if (enableDropcap && isFirstText) {
					sb.Append($"<p>{RenderDropcapTextRuns(ts.Runs, footnoteMap, endnotesFile, style)}</p>");
					isFirstText = false;
				} else {
					sb.Append($"<p>{RenderTextRuns(ts.Runs, footnoteMap, endnotesFile, style)}</p>");
					isFirstText = false;
				}
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
					sb.Append($"<p>{ds}</p>");
				}

				sb.Append("<hr class=\"divider\" aria-hidden=\"true\" />");
				break;
			case FootnoteSegmentModel fs:
				if (endnotesFile == null) {
					sb.Append($"<aside class=\"footnote-inline\">{RenderFootnoteContent(fs, style)}</aside>");
				}
				break;
			default:
				break;
		}
	}

	// ── Footnote map & text run rendering ─────────────────────────────────

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

	/// <summary>
	///     Builds a footnote map with numbering starting from a given counter.
	///     Used for consolidated modes where numbering continues across chapters.
	/// </summary>
	private static Dictionary<string, int> BuildFootnoteMap(List<SegmentModel> segments, ref int counter) {
		var footnoteMap = new Dictionary<string, int>();
		foreach (var segment in segments) {
			if (segment is not TextSegmentModel textSeg) {
				continue;
			}

			foreach (var run in textSeg.Runs) {
				if (!string.IsNullOrEmpty(run.FootnoteId) && !footnoteMap.ContainsKey(run.FootnoteId)) {
					footnoteMap[run.FootnoteId] = counter++;
				}
			}
		}

		return footnoteMap;
	}

	private static string FormatFootnoteLabel(int number, FootnoteStyle style) => style switch {
		FootnoteStyle.Parentheses => $"({number})",
		FootnoteStyle.Asterisk => new string('*', number),
		FootnoteStyle.SuperScript => number.ToString(),
		_ => $"[{number}]"
	};

	private static string RenderTextRuns(IEnumerable<TextRun> runs, Dictionary<string, int>? footnoteMap = null,
		string? endnotesFile = null, FootnoteStyle style = FootnoteStyle.Parentheses) {
		var sb = new StringBuilder();
		foreach (var run in runs) {
			var text = HttpUtility.HtmlEncode(run.Text);
			text = FormatText(text, run.IsBold, run.IsItalic);

			if (!string.IsNullOrEmpty(run.FootnoteId) && footnoteMap != null &&
				footnoteMap.TryGetValue(run.FootnoteId, out var footnoteNumber)) {
				var href = endnotesFile != null
					? $"{endnotesFile}#{run.FootnoteId}"
					: $"#{run.FootnoteId}";
				var idAttr = $" id=\"noteref-{run.FootnoteId}\"";
				var label = FormatFootnoteLabel(footnoteNumber, style);
				text +=
					$"<sup><a{idAttr} class=\"footnote-ref\" epub:type=\"noteref\" href=\"{href}\">{label}</a></sup>";
			}

			sb.Append(text);
		}

		return sb.ToString();
	}

	private static string RenderDropcapTextRuns(IEnumerable<TextRun> runs, Dictionary<string, int>? footnoteMap = null,
		string? endnotesFile = null, FootnoteStyle style = FootnoteStyle.Parentheses) {
		var runsList = runs.ToList();
		if (runsList.Count == 0) return string.Empty;

		var firstTextRunIndex = runsList.FindIndex(r => !string.IsNullOrEmpty(r.Text));
		if (firstTextRunIndex == -1) {
			return RenderTextRuns(runsList, footnoteMap, endnotesFile, style);
		}

		var sb = new StringBuilder();
		for (int i = 0; i < runsList.Count; i++) {
			var run = runsList[i];
			if (i == firstTextRunIndex) {
				var text = HttpUtility.HtmlEncode(run.Text);
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
						var href = endnotesFile != null ? $"{endnotesFile}#{run.FootnoteId}" : $"#{run.FootnoteId}";
						var idAttr = $" id=\"noteref-{run.FootnoteId}\"";
						var label = FormatFootnoteLabel(footnoteNumber, style);
						fullText += $"<sup><a{idAttr} class=\"footnote-ref\" epub:type=\"noteref\" href=\"{href}\">{label}</a></sup>";
					}
					sb.Append(fullText);
				} else {
					sb.Append(RenderTextRuns(new[] { run }, footnoteMap, endnotesFile, style));
				}
			} else {
				sb.Append(RenderTextRuns(new[] { run }, footnoteMap, endnotesFile, style));
			}
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

	// ── Inline footnotes (current behavior) ───────────────────────────────

	private static string RenderFootnoteContent(FootnoteSegmentModel footnote, FootnoteStyle style) {
		var sb = new StringBuilder();
		foreach (var textSegment in footnote.Segments) {
			sb.Append($"<p>{RenderTextRuns(textSegment.Runs, null, null, style)}</p>");
		}

		return sb.ToString();
	}

	private static string RenderFootnotesAtEnd(List<FootnoteSegmentModel> footnotes,
		Dictionary<string, int> footnoteMap, FootnoteStyle style) {
		if (footnotes == null || footnotes.Count == 0) {
			return string.Empty;
		}

		var sb = new StringBuilder();
		sb.AppendLine("<aside class=\"footnotes\" epub:type=\"footnotes\" role=\"doc-footnotes\">");

		foreach (var footnote in footnotes) {
			var footnoteIdStr = footnote.Id.ToString();
			if (!footnoteMap.TryGetValue(footnoteIdStr, out var footnoteNumber)) {
				continue;
			}

			sb.AppendLine($"<aside id=\"{footnoteIdStr}\" class=\"footnote-entry\" epub:type=\"footnote\" role=\"doc-footnote\">");
			var label = FormatFootnoteLabel(footnoteNumber, style);
			sb.Append($"<p><a class=\"endnote-backref\" href=\"#noteref-{footnoteIdStr}\">{label}</a> ");

			var contentSb = new StringBuilder();
			foreach (var textSeg in footnote.Segments) {
				contentSb.Append(RenderTextRuns(textSeg.Runs, null, null, style));
			}

			sb.Append(contentSb);

			sb.AppendLine("</p>");
			sb.AppendLine("</aside>");
		}

		sb.AppendLine("</aside>");
		return sb.ToString();
	}

	// ── Section dispatch ──────────────────────────────────────────────────

	public IReadOnlyList<NavEntry> RenderSection(
		BookExportContext context,
		ExportSectionDto section,
		IPackageBuilder builder) => section.Type switch {
			BookSection.Intro or BookSection.IntroPage => RenderIntro(context, section, builder),
			BookSection.Toc or BookSection.TableOfContents => RenderToc(builder, context.Structure),
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
				ImageFileMap = context.AssetFileMap,
				Localization = context.Structure.Localization
			});

		builder.AddPage("intro", html, PageRole.Intro);
		return [new NavEntry("intro", context.Structure.Localization.IntroductionLabel)];
	}

	private static IReadOnlyList<NavEntry> RenderToc(IPackageBuilder builder, ExportStructureDto structure) {
		// Content is empty because IPackageBuilder generates the nav.xhtml automatically
		builder.AddPage("toc", string.Empty, PageRole.TableOfContents);
		return [new NavEntry("toc", structure.Localization.TableOfContentsLabel)];
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
				Title = context.Structure.Localization.SummaryLabel,
				context.Series.Metadata?.Description,
				Section = section,
				ImageFileMap = context.AssetFileMap,
				Localization = context.Structure.Localization
			});

		builder.AddPage("description", html, PageRole.Description);
		return [new NavEntry("description", context.Structure.Localization.SummaryLabel)];
	}

	// ── RenderContent: tree-based traversal with footnote mode support ────

	private IReadOnlyList<NavEntry> RenderContent(BookExportContext context, IPackageBuilder builder) {
		var footnoteMode = context.Structure.FootnoteMode;
		var navEntries = new List<NavEntry>();

		// Walk the tree for canonical chapter ordering
		var volumeNodes = GetVolumeNodes(context.Tree);
		var globalEndnotes = new List<EndnoteEntry>();
		var globalCounter = 1;

		foreach (var volumeNode in volumeNodes) {
			if (!PrefixedId.TryToGuid(volumeNode.Id, EntityPrefix.Volume, out var volumeId)) {
				continue;
			}

			var volume = context.Volumes.FirstOrDefault(v => v.Id == volumeId);
			if (volume == null) {
				continue;
			}

			// Render volume title page
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
					volume.Metadata?.Isbn,
					Localization = context.Structure.Localization
				});
			var volFileName = builder.AddPage(volId, volHtml, PageRole.VolumeTitle);

			// Pre-resolve endnotes filename for cross-file links
			var endnotesPageId = $"endnotes_vol_{volume.Id:N}";
			string? endnotesFileName = null;
			if (footnoteMode != FootnoteMode.Inline) {
				endnotesFileName = footnoteMode == FootnoteMode.Global
					? "endnotes_001.xhtml"
					: $"endnotes_{context.Volumes.IndexOf(volume) + 1:D3}.xhtml";
			}

			var children = new List<NavEntry>();
			var volumeEndnotes = new List<EndnoteEntry>();
			var volumeCounter = 1;

			// Walk chapter nodes from tree (canonical order)
			if (!context.ChapterMap.TryGetValue(volumeId, out var chapterModels)) {
				chapterModels = [];
			}

			foreach (var chapterNode in volumeNode.Children) {
				if (!PrefixedId.TryToGuid(chapterNode.Id, EntityPrefix.Chapter, out var chapterId)) {
					continue;
				}

				var chapter = chapterModels.FirstOrDefault(c => c.Id == chapterId);
				if (chapter?.ContentData?.Segments == null) {
					continue;
				}

				var chId = chapter.Id.ToString();
				var segments = chapter.ContentData.Segments;
				var footnotes = chapter.ContentData.Footnotes;

				string renderedContent;
				string chFileName;

				if (footnoteMode == FootnoteMode.Inline) {
					// Current behavior: footnotes inline at end of chapter
					renderedContent = RenderSegments(segments, footnotes, context.AssetFileMap, context.Structure.FootnoteStyle, context.Structure.EnableDropcap);
					var chHtml = templateEngine.Render("epub_chapter",
						new { chapter.Title, RenderedContent = renderedContent, Localization = context.Structure.Localization });
					chFileName = builder.AddPage(chId, chHtml);
				} else {
					// Consolidated: cross-file href, no inline footnotes
					var segmentList = segments.ToList();
					if (footnotes is { Count: > 0 }) {
						segmentList = StripTrailingFootnoteHeader(segmentList, context.Structure.Localization);
					}
					ref var counter = ref (footnoteMode == FootnoteMode.Global ? ref globalCounter : ref volumeCounter);
					var footnoteMap = BuildFootnoteMap(segmentList, ref counter);

					// Render segments with cross-file noteref links
					var sb = new StringBuilder();
					var isFirstText = true;
					foreach (var segment in segmentList) {
						RenderSegmentBody(sb, segment, footnoteMap, context.AssetFileMap, endnotesFileName, ref isFirstText, context.Structure.EnableDropcap, context.Structure.FootnoteStyle);
					}

					renderedContent = sb.ToString();
					var chHtml = templateEngine.Render("epub_chapter",
						new { chapter.Title, RenderedContent = renderedContent, Localization = context.Structure.Localization });
					chFileName = builder.AddPage(chId, chHtml);

					// Collect footnotes into accumulator
					if (footnotes is { Count: > 0 }) {
						foreach (var fn in footnotes) {
							var fnIdStr = fn.Id.ToString();
							if (footnoteMap.TryGetValue(fnIdStr, out var number)) {
								volumeEndnotes.Add(new EndnoteEntry(fnIdStr, number, fn, chapter.Title, chFileName));
							}
						}
					}
				}

				children.Add(new NavEntry(chFileName, chapter.Title));
			}

			// Emit endnotes page for this volume
			if (footnoteMode == FootnoteMode.PerVolume && volumeEndnotes.Count > 0) {
				EmitEndnotesPage(builder, endnotesPageId, volumeEndnotes, context.Structure.EndnoteGrouping,
					$"{context.Structure.Localization.FootnoteLabel} {volume.Title}", context.Structure.FootnoteStyle, context.Structure.Localization);
			} else if (footnoteMode == FootnoteMode.Global) {
				globalEndnotes.AddRange(volumeEndnotes);
			}

			navEntries.Add(new NavEntry(volFileName, volume.Title, children));
		}

		// Emit single global endnotes page
		if (footnoteMode == FootnoteMode.Global && globalEndnotes.Count > 0) {
			EmitEndnotesPage(builder, "endnotes_global", globalEndnotes, context.Structure.EndnoteGrouping,
				context.Structure.Localization.FootnoteLabel, context.Structure.FootnoteStyle, context.Structure.Localization);
		}

		return navEntries;
	}

	// ── Tree helpers ──────────────────────────────────────────────────────

	private static IReadOnlyList<BookTreeNodeDto> GetVolumeNodes(BookTreeDto tree) {
		var seriesNode = tree.Root.Children.FirstOrDefault();
		return seriesNode?.Children ?? [];
	}

	// ── Consolidated endnotes rendering ───────────────────────────────────

	private void EmitEndnotesPage(IPackageBuilder builder, string pageId,
		List<EndnoteEntry> entries, EndnoteGrouping grouping, string title, FootnoteStyle style, ExportLocalizationDto localization) {
		var renderedContent = RenderEndnotesContent(entries, grouping, style);
		var html = templateEngine.Render("epub_endnotes",
			new { Title = title, RenderedContent = renderedContent, Localization = localization });
		builder.AddPage(pageId, html, PageRole.Endnotes);
	}

	private static string RenderEndnotesContent(List<EndnoteEntry> entries, EndnoteGrouping grouping, FootnoteStyle style) {
		var sb = new StringBuilder();

		if (grouping == EndnoteGrouping.ByChapter) {
			var groups = entries.GroupBy(e => e.ChapterTitle,
				(key, group) => (Title: key, Entries: group.ToList()));
			foreach (var (chapterTitle, chapterEntries) in groups) {
				sb.AppendLine($"<h3>{HttpUtility.HtmlEncode(chapterTitle)}</h3>");
				foreach (var entry in chapterEntries) {
					AppendEndnoteEntry(sb, entry, style);
				}
			}
		} else {
			foreach (var entry in entries) {
				AppendEndnoteEntry(sb, entry, style);
			}
		}

		return sb.ToString();
	}

	private static void AppendEndnoteEntry(StringBuilder sb, EndnoteEntry entry, FootnoteStyle style) {
		sb.AppendLine($"<aside id=\"{entry.FootnoteId}\" class=\"endnote-entry\" epub:type=\"footnote\" role=\"doc-footnote\">");
		var label = FormatFootnoteLabel(entry.Number, style);
		sb.Append($"<p><a class=\"endnote-backref\" href=\"{entry.ChapterFileName}#noteref-{entry.FootnoteId}\">{label}</a> ");

		foreach (var textSeg in entry.Footnote.Segments) {
			sb.Append(RenderTextRuns(textSeg.Runs, null, null, style));
		}

		sb.AppendLine("</p>");
		sb.AppendLine("</aside>");
	}

	// ── Misc helpers ──────────────────────────────────────────────────────

	private static List<SegmentModel> StripTrailingFootnoteHeader(List<SegmentModel> segments, ExportLocalizationDto localization) {
		if (segments.Count == 0) {
			return segments;
		}

		var result = new List<SegmentModel>(segments);
		for (var i = result.Count - 1; i >= 0; i--) {
			var segment = result[i];
			if (segment is TextSegmentModel ts) {
				var combinedText = string.Concat(ts.Runs.Select(r => r.Text)).Trim();
				if (combinedText.Equals("Ghi chú", StringComparison.OrdinalIgnoreCase) ||
					combinedText.Equals("Ghi chú:", StringComparison.OrdinalIgnoreCase) ||
					combinedText.Equals("Chú thích", StringComparison.OrdinalIgnoreCase) ||
					combinedText.Equals("Chú thích:", StringComparison.OrdinalIgnoreCase) ||
					combinedText.Equals(localization.FootnoteLabel.TrimEnd(':'), StringComparison.OrdinalIgnoreCase) ||
					combinedText.Equals(localization.FootnoteLabel, StringComparison.OrdinalIgnoreCase)) {
					result.RemoveAt(i);
					break; // Only remove the first trailing header we find from the end
				}
			}
		}
		return result;
	}

	private static string? ResolveCoverLocalPath(BookExportContext context) {
		if (context.CoverAsset == null) {
			return null;
		}

		var coverFileName = ImageAssetCollector.BuildExportFileName(context.CoverAsset.OriginalFileName, 0);
		return $"{EpubConstants.Paths.IMAGES_FOLDER}{coverFileName}";
	}

	[LoggerMessage(LogLevel.Information, "Skipping separate description (already in intro)")]
	partial void LogSkippingDescription();
}
