namespace Grimoire.Infrastructure.Export.Epub;

using System.Text;
using System.Web;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;

/// <summary>
///     Renders text segments to HTML
/// </summary>
public class TextSegmentRenderer : ISegmentRenderer {
	public bool CanRender(SegmentModel segment) => segment is TextSegmentModel;

	public string Render(SegmentModel segment, SegmentRenderContext context) {
		if (segment is not TextSegmentModel textSegment) {
			return string.Empty;
		}

		var sb = new StringBuilder();
		sb.Append("<p>");

		foreach (var run in textSegment.Runs) {
			var text = HttpUtility.HtmlEncode(run.Text);

			text = FormatText(text, run.IsBold, run.IsItalic);
			sb.Append(text);

			if (!string.IsNullOrEmpty(run.FootnoteId) &&
				context.FootnoteMap != null &&
				context.FootnoteMap.TryGetValue(run.FootnoteId, out var number)) {
				sb.Append(
					$" <a class=\"footnote-link\" epub:type=\"noteref\" href=\"#{run.FootnoteId}\">({number})</a>");
			}
		}

		sb.AppendLine("</p>");
		return sb.ToString();
	}

	private static string FormatText(string text, bool isBold, bool isItalic) =>
		(isBold, isItalic) switch {
			(true, true) => $"<strong><em>{text}</em></strong>",
			(true, false) => $"<strong>{text}</strong>",
			(false, true) => $"<em>{text}</em>",
			_ => text
		};
}

/// <summary>
///     Renders image segments to HTML
/// </summary>
public class ImageSegmentRenderer : ISegmentRenderer {
	public bool CanRender(SegmentModel segment) => segment is ImageSegmentModel;

	public string Render(SegmentModel segment, SegmentRenderContext context) {
		if (segment is not ImageSegmentModel imageSegment) {
			return string.Empty;
		}

		var imageSrc = ResolveImageSource(imageSegment, context.ImageFileMap);
		var sb = new StringBuilder();

		sb.Append("<p><img alt=\"");
		if (!string.IsNullOrEmpty(imageSegment.Caption)) {
			sb.Append(HttpUtility.HtmlEncode(imageSegment.Caption));
		}

		sb.AppendLine($"\" src=\"{imageSrc}\"/></p>");

		return sb.ToString();
	}

	private static string ResolveImageSource(ImageSegmentModel image, Dictionary<string, string>? imageFileMap) {
		if (imageFileMap != null && imageFileMap.TryGetValue(image.AssetKey, out var mappedFileName)) {
			return $"images/{mappedFileName}";
		}

		if (Guid.TryParse(image.AssetKey, out var assetId)) {
			return $"images/img_{assetId}.jpg";
		}

		return $"images/{image.AssetKey}";
	}
}

/// <summary>
///     Renders divider segments to HTML
/// </summary>
public class DividerSegmentRenderer : ISegmentRenderer {
	public bool CanRender(SegmentModel segment) => segment is DividerSegmentModel;

	public string Render(SegmentModel segment, SegmentRenderContext context) {
		if (segment is not DividerSegmentModel divider) {
			return string.Empty;
		}

		return $"<p>{HttpUtility.HtmlEncode(divider.Style)}</p>\n";
	}
}
