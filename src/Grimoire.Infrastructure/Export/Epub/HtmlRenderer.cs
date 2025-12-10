namespace Grimoire.Infrastructure.Export.Epub;

using System.Text;
using System.Web;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;

/// <summary>
///     Renders HTML content from domain models for EPUB generation
/// </summary>
public class HtmlRenderer {
	private readonly List<ISegmentRenderer> _segmentRenderers;

	public HtmlRenderer() {
		_segmentRenderers = [
			new TextSegmentRenderer(),
			new ImageSegmentRenderer(),
			new DividerSegmentRenderer()
		];
	}

	/// <summary>
	///     Renders a chapter to XHTML
	/// </summary>
	public string RenderChapter(ChapterViewModel chapter) {
		var html = new StringBuilder();
		var footnoteMap = BuildFootnoteMap(chapter.Segments);

		AppendXmlHeader(html);
		AppendHtmlHead(html, chapter.Title);
		html.AppendLine("<body>");
		html.AppendLine($"<h2>{HttpUtility.HtmlEncode(chapter.Title)}</h2>");
		html.AppendLine("<div class=\"long-text no-select text-justify\" id=\"chapter-content\">");

		var context = new SegmentRenderContext { ImageFileMap = chapter.ImageFileMap, FootnoteMap = footnoteMap };

		foreach (var segment in chapter.Segments) {
			html.Append(RenderSegment(segment, context));
		}

		html.AppendLine("</div>");

		RenderFootnotes(html, chapter.Footnotes, footnoteMap);

		html.AppendLine("</body>");
		html.AppendLine("</html>");

		return html.ToString();
	}

	private Dictionary<string, int> BuildFootnoteMap(List<SegmentModel> segments) {
		var footnoteMap = new Dictionary<string, int>();
		var footnoteCounter = 1;

		foreach (var segment in segments) {
			if (segment is TextSegmentModel textSeg) {
				foreach (var run in textSeg.Runs) {
					if (!string.IsNullOrEmpty(run.FootnoteId) && !footnoteMap.ContainsKey(run.FootnoteId)) {
						footnoteMap[run.FootnoteId] = footnoteCounter++;
					}
				}
			}
		}

		return footnoteMap;
	}

	private string RenderSegment(SegmentModel segment, SegmentRenderContext context) {
		var renderer = _segmentRenderers.FirstOrDefault(r => r.CanRender(segment));
		return renderer?.Render(segment, context) ?? string.Empty;
	}

	private void RenderFootnotes(StringBuilder html, List<FootnoteSegmentModel>? footnotes,
		Dictionary<string, int> footnoteMap) {
		if (footnotes == null || footnotes.Count == 0) {
			return;
		}

		foreach (var footnote in footnotes) {
			var footnoteIdStr = footnote.Id.ToString();
			if (!footnoteMap.TryGetValue(footnoteIdStr, out _)) {
				continue;
			}

			html.AppendLine($"<aside class=\"footnote-content\" epub:type=\"footnote\" id=\"{footnoteIdStr}\">");
			html.AppendLine("<div class=\"note-header\">Ghi chú:</div>");

			var context = new SegmentRenderContext { ImageFileMap = null, FootnoteMap = null };
			foreach (var textSeg in footnote.Segments) {
				var renderer = new TextSegmentRenderer();
				html.Append(renderer.Render(textSeg, context));
			}

			html.AppendLine("</aside>");
		}
	}

	/// <summary>
	///     Renders the intro/title page
	/// </summary>
	public string RenderIntro(IntroViewModel intro) {
		var html = new StringBuilder();

		AppendXmlHeader(html);
		AppendHtmlHead(html, "Giới thiệu");
		html.AppendLine("<body>");
		html.AppendLine("<div class=\"title-page\">");
		html.AppendLine($"<div class=\"book-title\">{HttpUtility.HtmlEncode(intro.BookTitle)}</div>");

		if (!string.IsNullOrEmpty(intro.Author)) {
			html.AppendLine($"<div class=\"book-author\">Tác giả: {HttpUtility.HtmlEncode(intro.Author)}</div>");
		}

		if (!string.IsNullOrEmpty(intro.CoverLocalPath)) {
			html.AppendLine("<div class=\"book-cover\">");
			html.AppendLine($"<img src=\"{intro.CoverLocalPath}\" alt=\"Cover Image\" />");
			html.AppendLine("</div>");
		}

		if (intro.Tags != null && intro.Tags.Count > 0) {
			html.AppendLine("<div class=\"tags\">");
			foreach (var tag in intro.Tags) {
				html.AppendLine($"<span class=\"tag-item\">{HttpUtility.HtmlEncode(tag)}</span>");
			}

			html.AppendLine("</div>");
		}

		html.AppendLine("</div>");

		if (intro.Description != null && intro.Description.Count > 0) {
			html.AppendLine("<div class=\"front-matter\">");
			html.AppendLine("<div class=\"section-title\">Tóm tắt</div>");
			html.AppendLine("<div class=\"description text-justify\">");

			var context = new SegmentRenderContext { ImageFileMap = null, FootnoteMap = null };
			var renderer = new TextSegmentRenderer();
			foreach (var segment in intro.Description) {
				html.Append(renderer.Render(segment, context));
			}

			html.AppendLine("</div>");
			html.AppendLine("</div>");
		}

		html.AppendLine("</body>");
		html.AppendLine("</html>");

		return html.ToString();
	}

	/// <summary>
	///     Renders table of contents navigation
	/// </summary>
	public string RenderToc(List<NavPoint> navPoints) {
		var html = new StringBuilder();

		AppendXmlHeader(html);
		AppendHtmlHead(html, "Mục lục");
		html.AppendLine("<body>");
		html.AppendLine("<nav epub:type=\"toc\" id=\"toc\" role=\"doc-toc\">");
		html.AppendLine("<h2>Mục lục</h2>");
		html.AppendLine("<ol>");

		foreach (var nav in navPoints) {
			html.AppendLine(RenderNavPoint(nav));
		}

		html.AppendLine("</ol>");
		html.AppendLine("</nav>");
		html.AppendLine("</body>");
		html.AppendLine("</html>");

		return html.ToString();
	}

	private static string RenderNavPoint(NavPoint nav) {
		var sb = new StringBuilder();
		sb.AppendLine("<li>");
		sb.AppendLine($"<a href=\"{nav.ContentSrc}\">{HttpUtility.HtmlEncode(nav.Title)}</a>");

		if (nav.Children != null && nav.Children.Count > 0) {
			sb.AppendLine("<ol>");
			foreach (var child in nav.Children) {
				sb.Append(RenderNavPoint(child));
			}

			sb.AppendLine("</ol>");
		}

		sb.AppendLine("</li>");
		return sb.ToString();
	}

	private static void AppendXmlHeader(StringBuilder html) {
		html.AppendLine("<?xml version='1.0' encoding='utf-8'?>");
		html.AppendLine("<!DOCTYPE html>");
		html.AppendLine(
			"<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\" lang=\"vi\" xml:lang=\"vi\">");
	}

	private static void AppendHtmlHead(StringBuilder html, string title) {
		html.AppendLine("<head>");
		html.AppendLine($"<title>{HttpUtility.HtmlEncode(title)}</title>");
		html.AppendLine("<link href=\"style.css\" rel=\"stylesheet\" type=\"text/css\"/>");
		html.AppendLine("</head>");
	}
}
