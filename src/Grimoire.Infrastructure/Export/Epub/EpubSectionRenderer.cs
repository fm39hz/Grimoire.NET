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
		var segmentList = segments.ToList();
		if (segmentList.Count == 0) {
			return string.Empty;
		}

		var footnoteMap = BuildFootnoteMap(segmentList);
		var sb = new StringBuilder();

		foreach (var segment in segmentList) {
			RenderSegmentBody(sb, segment, footnoteMap, assetMap);
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

	// ── Segment rendering (shared between inline and consolidated) ────────

	private static void RenderSegmentBody(StringBuilder sb, SegmentModel segment,
		Dictionary<string, int> footnoteMap, IReadOnlyDictionary<string, string>? assetMap,
		string? endnotesFile = null) {
		switch (segment) {
			case TextSegmentModel ts:
				sb.Append($"<p>{RenderTextRuns(ts.Runs, footnoteMap, endnotesFile)}</p>");
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

	private static string RenderTextRuns(IEnumerable<TextRun> runs, Dictionary<string, int>? footnoteMap = null,
		string? endnotesFile = null) {
		var sb = new StringBuilder();
		foreach (var run in runs) {
			var text = HttpUtility.HtmlEncode(run.Text);
			text = FormatText(text, run.IsBold, run.IsItalic);

			if (!string.IsNullOrEmpty(run.FootnoteId) && footnoteMap != null &&
				footnoteMap.TryGetValue(run.FootnoteId, out var footnoteNumber)) {
				var href = endnotesFile != null
					? $"{endnotesFile}#{run.FootnoteId}"
					: $"#{run.FootnoteId}";
				var idAttr = endnotesFile != null
					? $" id=\"noteref-{run.FootnoteId}\""
					: "";
				text +=
					$"<a{idAttr} class=\"footnote-ref\" epub:type=\"noteref\" href=\"{href}\">[{footnoteNumber}]</a>";
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

	// ── Inline footnotes (current behavior) ───────────────────────────────

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

	// ── Section dispatch ──────────────────────────────────────────────────

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
					volume.Metadata?.Isbn
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
					renderedContent = RenderSegments(segments, footnotes, context.AssetFileMap);
					var chHtml = templateEngine.Render("epub_chapter",
						new { chapter.Title, RenderedContent = renderedContent });
					chFileName = builder.AddPage(chId, chHtml);
				} else {
					// Consolidated: cross-file href, no inline footnotes
					var segmentList = segments.ToList();
					ref var counter = ref (footnoteMode == FootnoteMode.Global ? ref globalCounter : ref volumeCounter);
					var footnoteMap = BuildFootnoteMap(segmentList, ref counter);

					// Render segments with cross-file noteref links
					var sb = new StringBuilder();
					foreach (var segment in segmentList) {
						RenderSegmentBody(sb, segment, footnoteMap, context.AssetFileMap, endnotesFileName);
					}

					renderedContent = sb.ToString();
					var chHtml = templateEngine.Render("epub_chapter",
						new { chapter.Title, RenderedContent = renderedContent });
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
					$"{EpubConstants.LocalizedText.FOOTNOTE} {volume.Title}");
			} else if (footnoteMode == FootnoteMode.Global) {
				globalEndnotes.AddRange(volumeEndnotes);
			}

			navEntries.Add(new NavEntry(volFileName, volume.Title, children));
		}

		// Emit single global endnotes page
		if (footnoteMode == FootnoteMode.Global && globalEndnotes.Count > 0) {
			EmitEndnotesPage(builder, "endnotes_global", globalEndnotes, context.Structure.EndnoteGrouping,
				EpubConstants.LocalizedText.FOOTNOTE);
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
		List<EndnoteEntry> entries, EndnoteGrouping grouping, string title) {
		var renderedContent = RenderEndnotesContent(entries, grouping);
		var html = templateEngine.Render("epub_endnotes",
			new { Title = title, RenderedContent = renderedContent });
		builder.AddPage(pageId, html, PageRole.Endnotes);
	}

	private static string RenderEndnotesContent(List<EndnoteEntry> entries, EndnoteGrouping grouping) {
		var sb = new StringBuilder();

		if (grouping == EndnoteGrouping.ByChapter) {
			var groups = entries.GroupBy(e => e.ChapterTitle,
				(key, group) => (Title: key, Entries: group.ToList()));
			foreach (var (chapterTitle, chapterEntries) in groups) {
				sb.AppendLine($"<h3>{HttpUtility.HtmlEncode(chapterTitle)}</h3>");
				foreach (var entry in chapterEntries) {
					AppendEndnoteEntry(sb, entry);
				}
			}
		} else {
			foreach (var entry in entries) {
				AppendEndnoteEntry(sb, entry);
			}
		}

		return sb.ToString();
	}

	private static void AppendEndnoteEntry(StringBuilder sb, EndnoteEntry entry) {
		sb.AppendLine($"<div id=\"{entry.FootnoteId}\" class=\"endnote-entry\" epub:type=\"footnote\">");
		sb.Append($"<p>[{entry.Number}] ");

		foreach (var textSeg in entry.Footnote.Segments) {
			sb.Append(RenderTextRuns(textSeg.Runs));
		}

		sb.Append($" <a class=\"endnote-backref\" href=\"{entry.ChapterFileName}#noteref-{entry.FootnoteId}\">↩</a>");
		sb.AppendLine("</p>");
		sb.AppendLine("</div>");
	}

	// ── Misc helpers ──────────────────────────────────────────────────────

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
