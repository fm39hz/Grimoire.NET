namespace Grimoire.Infrastructure.Export.Common;

using System.Text;
using System.Web;
using Application.Common;
using Application.Dto.Book;
using Application.Export;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Scriban.Runtime;

public class ScribanExportFunctions : ScriptObject {
	public static string ToFormat(object input, string format, object? footnotes = null, object? assetMap = null) => format.ToLowerInvariant() switch {
		"markdown" => ToMarkdown(input, footnotes, assetMap),
		"html" => ToHtml(input, footnotes, assetMap),
		"epub" => ToHtml(input, footnotes, assetMap), // EPUB uses HTML content
		_ => string.Empty
	};

	public static string ToMarkdown(object input, object? footnotes = null, object? assetMap = null) {
		var footnoteList = footnotes as List<FootnoteSegmentModel>;

		var segmentList = input switch {
			List<SegmentModel> segmentListModel => segmentListModel,
			List<TextSegmentModel> textSegmentList => textSegmentList.Select(x => x as SegmentModel).ToList(),
			_ => null
		};

		// Sử dụng pattern matching để xử lý list đặc thù nếu cần
		return segmentList == null
			? string.Empty
			: SegmentMarkdownConverter.ConvertToMarkdown(segmentList, footnoteList);
	}

	public static string ToHtml(object input, object? footnotes = null, object? assetMap = null) {
		// Convert input to the expected types

		var segmentList = input switch {
			List<SegmentModel> segmentListModel => segmentListModel,
			List<TextSegmentModel> textSegmentList => textSegmentList.Select(SegmentModel (x) => x).ToList(),
			_ => null
		};

		if (segmentList == null) {
			return string.Empty;
		}

		var assetDict = ConvertToDictionary(assetMap);

		// Build footnote map to track footnote references in the segments
		var footnoteMap = BuildFootnoteMap(segmentList);

		var sb = new StringBuilder();
		foreach (var segment in segmentList) {
			switch (segment) {
				case TextSegmentModel ts:
					sb.Append($"<p>{RenderTextRuns(ts.Runs, footnoteMap)}</p>");
					break;
				case ImageSegmentModel ism:
					var url = assetDict?.TryGetValue(ism.AssetKey, out var fileName) == true
						? $"images/{fileName}"
						: ism.AssetKey;
					var encodedCaption = HttpUtility.HtmlEncode(ism.Caption ?? string.Empty);
					sb.Append(
						$"<figure><img src=\"{HttpUtility.HtmlEncode(url)}\" alt=\"{encodedCaption}\"/><figcaption>{encodedCaption}</figcaption></figure>");
					break;
				case DividerSegmentModel ds:
					sb.Append($"<hr class=\"divider\" aria-hidden=\"true\" />");
					break;
				case FootnoteSegmentModel fs:
					// Footnotes are typically rendered separately, not inline
					// But if they need to be rendered inline, we can do so here
					sb.Append($"<aside class=\"footnote-inline\">{RenderFootnoteContent(fs, assetDict)}</aside>");
					break;
				default:
					break;
			}
		}

		// Also render the footnotes at the end if provided
		if (footnotes is List<FootnoteSegmentModel> { Count: > 0 } footnoteList) {
			sb.Append(RenderFootnotesAtEnd(footnoteList, footnoteMap));
		}

		return sb.ToString();
	}

	// Helper method to convert object to dictionary
	private static Dictionary<string, string>? ConvertToDictionary(object? obj) {
		switch (obj) {
			case Dictionary<string, string> dict:
				return dict;
			case IReadOnlyDictionary<string, string> readOnlyDict: {
					var newDict = new Dictionary<string, string>();
					foreach (var kvp in readOnlyDict) {
						newDict[kvp.Key] = kvp.Value;
					}

					return newDict;
				}
			default:
				return obj as Dictionary<string, string>;
		}
	}

	private static string RenderTextRuns(IEnumerable<TextRun> runs, Dictionary<string, int>? footnoteMap = null) {
		var sb = new StringBuilder();
		foreach (var run in runs) {
			var text = HttpUtility.HtmlEncode(run.Text);
			text = FormatText(text, run.IsBold, run.IsItalic);

			// Handle footnote references if footnoteMap exists
			if (!string.IsNullOrEmpty(run.FootnoteId) && footnoteMap != null &&
				footnoteMap.TryGetValue(run.FootnoteId, out var footnoteNumber)) {
				text +=
					$"<a class=\"footnote-ref\" epub:type=\"noteref\" href=\"#{run.FootnoteId}\">[{footnoteNumber}]</a>";
			}

			sb.Append(text);
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

	private static string RenderFootnoteContent(FootnoteSegmentModel footnote, Dictionary<string, string>? assetMap) {
		var sb = new StringBuilder();
		// Footnote segments only contain TextSegmentModel according to the definition
		// Each segment in footnote.Segments is definitely a TextSegmentModel
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

			// Render the footnote content
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

	private static string FormatText(string text, bool isBold, bool isItalic) =>
		(isBold, isItalic) switch {
			(true, true) => $"<strong><em>{text}</em></strong>",
			(true, false) => $"<strong>{text}</strong>",
			(false, true) => $"<em>{text}</em>",
			_ => text
		};

	public static string GenerateAnchor(string title) =>
		title?.ToLowerInvariant().Replace(" ", "-") ?? string.Empty;

	public static bool ShouldShowDescriptionInIntro(ExportSectionDto? section) =>
		!ExportUtilities.IsSplitDescriptionEnabled(section);

	public static bool IsSectionType(ExportSectionDto section, params string[] types) =>
		types.Contains(section.Type.ToString(), StringComparer.OrdinalIgnoreCase);

	public static bool IsSectionTypeExact(ExportSectionDto section, string targetType) =>
		Enum.TryParse<BookSection>(targetType, true, out var targetEnum) &&
		section.Type == targetEnum;
}
