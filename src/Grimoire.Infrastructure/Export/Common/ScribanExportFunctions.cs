namespace Grimoire.Infrastructure.Export.Common;

using System.Text;
using Application.Common;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Scriban.Runtime;

public class ScribanExportFunctions : ScriptObject {
	public static string ToMarkdown(List<SegmentModel> input, List<FootnoteSegmentModel>? footnotes,
		IReadOnlyDictionary<string, string>? assetMap) {
		if (input == null) return string.Empty;

		// Sử dụng pattern matching để xử lý list đặc thù nếu cần
		return SegmentMarkdownConverter.ConvertToMarkdown(input, footnotes);
	}

	public static string ToHtml(List<SegmentModel> input, IReadOnlyDictionary<string, string>? assetMap) {
		if (input == null) return string.Empty;

		var sb = new StringBuilder();
		foreach (var segment in input) {
			switch (segment) {
				case TextSegmentModel ts:
					sb.Append($"<p>{RenderTextRuns(ts.Runs)}</p>");
					break;
				case ImageSegmentModel ism:
					var url = (assetMap?.TryGetValue(ism.AssetKey, out var fileName) == true)? fileName : ism.AssetKey;
					sb.Append(
						$"<figure><img src=\"{url}\" alt=\"{ism.Caption}\"/><figcaption>{ism.Caption}</figcaption></figure>");
					break;
			}
		}

		return sb.ToString();
	}

	private static string RenderTextRuns(IEnumerable<TextRun> runs) {
		var sb = new StringBuilder();
		foreach (var run in runs) {
			var text = run.Text;
			switch (run.IsBold) {
				case true when run.IsItalic:
					text = $"<strong><em>{text}</em></strong>";
					break;
				case true:
					text = $"<strong>{text}</strong>";
					break;
				default: {
					if (run.IsItalic) {
						text = $"<em>{text}</em>";
					}

					break;
				}
			}

			sb.Append(text);
		}

		return sb.ToString();
	}

	public static string GenerateAnchor(string title) =>
		title?.ToLowerInvariant().Replace(" ", "-") ?? string.Empty;
}
