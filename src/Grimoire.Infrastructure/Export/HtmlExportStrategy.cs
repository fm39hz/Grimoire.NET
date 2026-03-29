namespace Grimoire.Infrastructure.Export;

using System.Text;
using Application.Dto.Book;
using Application.Export;
using Application.Service.Strategy;
using Common;

/// <summary>
///     Strategy for exporting series to HTML format
/// </summary>
public class HtmlExportStrategy : IExportStrategy {
	public ExportFormat Format => ExportFormat.Html;

	public async Task<ExportResult> ExportAsync(BookExportContext context) {
		try {
			var html = new StringBuilder();

			html.AppendLine("<!DOCTYPE html>");
			html.AppendLine("<html>");
			html.AppendLine("<head>");
			html.AppendLine($"<title>{context.Series.Title}</title>");

			// Add CSS
			if (context.Structure.GlobalCss != null) {
				html.AppendLine("<style>");
				html.AppendLine(context.Structure.GlobalCss);
				html.AppendLine("</style>");
			}
			else {
				html.AppendLine("<style>");
				html.AppendLine("body { font-family: sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; }");
				html.AppendLine(".section { margin: 2em 0; padding: 1em; }");
				html.AppendLine(".intro { border-bottom: 2px solid #ccc; }");
				html.AppendLine(".toc { padding: 1em; }");
				html.AppendLine(".toc ul { list-style: none; }");
				html.AppendLine(".toc a { text-decoration: none; }");
				html.AppendLine(".chapter { margin-top: 2em; }");
				html.AppendLine("</style>");
			}

			html.AppendLine("</head>");
			html.AppendLine("<body>");

			// Process structure sections
			foreach (var section in context.Structure.Sections) {
				ProcessSection(section, context, html);
			}

			html.AppendLine("</body>");
			html.AppendLine("</html>");

			var memoryStream = new MemoryStream();
			await using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
			await writer.WriteAsync(html.ToString());
			await writer.FlushAsync();
			memoryStream.Position = 0;

			var fileName = $"{ExportUtilities.SanitizeFileName(context.Series.Title)}.html";

			return ExportResult.Ok(memoryStream, fileName, "text/html");
		}
		catch (Exception ex) {
			return ExportResult.Fail(ex.Message);
		}
	}

	private static void ProcessSection(ExportSectionDto section, BookExportContext context, StringBuilder html) {
		switch (section.Type) {
			case BookSection.IntroPage:
			case BookSection.Intro:
				ProcessIntroSection(section, context.Series, html);
				break;

			case BookSection.Toc:
			case BookSection.TableOfContents:
				ProcessTocSection(section, context.Volumes, html);
				break;

			case BookSection.Content:
			case BookSection.Chapters:
				ProcessContentSection(section, context, html);
				break;

			case BookSection.Description:
				ProcessDescriptionSection(section, context.Series, html);
				break;
			case BookSection.Unknown:
				break;
			default:
				break;
		}
	}

	private static void ProcessIntroSection(ExportSectionDto? section, Domain.Entity.Book.SeriesModel series, StringBuilder html) {
		var splitDescription = ExportUtilities.IsSplitDescriptionEnabled(section);

		html.AppendLine("<div class='section intro' id='intro'>");

		if (section?.CustomCss != null) {
			html.AppendLine($"<style>{section.CustomCss}</style>");
		}

		html.AppendLine($"<h1>{series.Title}</h1>");

		if (series.Metadata?.Authors != null && series.Metadata.Authors.Count > 0) {
			html.AppendLine($"<p><strong>Author:</strong> {string.Join(", ", series.Metadata.Authors)}</p>");
		}

		if (!splitDescription && series.Metadata?.Description != null && series.Metadata.Description.Count > 0) {
			html.AppendLine("<div class='description'>");
			html.AppendLine("<h2>Description</h2>");
			foreach (var desc in series.Metadata.Description) {
				foreach (var run in desc.Runs) {
					html.AppendLine($"<p>{run.Text}</p>");
				}
			}

			html.AppendLine("</div>");
		}

		if (series.Metadata?.Tags != null && series.Metadata.Tags.Count > 0) {
			html.AppendLine("<p><strong>Tags:</strong> " + string.Join(", ", series.Metadata.Tags) + "</p>");
		}

		html.AppendLine("</div>");
	}

	private static void ProcessDescriptionSection(ExportSectionDto? section, Domain.Entity.Book.SeriesModel series, StringBuilder html) {
		var splitDescription = ExportUtilities.IsSplitDescriptionEnabled(section);
		if (splitDescription || series.Metadata?.Description == null || series.Metadata.Description.Count == 0) {
			return;
		}

		html.AppendLine("<div class='section description' id='description'>");

		if (section?.CustomCss != null) {
			html.AppendLine($"<style>{section.CustomCss}</style>");
		}

		html.AppendLine("<h2>Description</h2>");
		foreach (var desc in series.Metadata.Description) {
			foreach (var run in desc.Runs) {
				html.AppendLine($"<p>{run.Text}</p>");
			}
		}

		html.AppendLine("</div>");
	}

	private static void ProcessTocSection(ExportSectionDto? section, List<Domain.Entity.Book.VolumeModel> volumeList, StringBuilder html) {
		html.AppendLine("<div class='section toc' id='toc'>");

		if (section?.CustomCss != null) {
			html.AppendLine($"<style>{section.CustomCss}</style>");
		}

		html.AppendLine("<h2>Table of Contents</h2>");
		html.AppendLine("<ul>");

		foreach (var volume in volumeList) {
			html.AppendLine($"<li><a href='#volume-{volume.Id}'>{volume.Title}</a></li>");
		}

		html.AppendLine("</ul>");
		html.AppendLine("</div>");
	}

	private static void ProcessContentSection(ExportSectionDto? section, BookExportContext context,
		StringBuilder html) {
		html.AppendLine("<div class='section content' id='content'>");

		if (section?.CustomCss != null) {
			html.AppendLine($"<style>{section.CustomCss}</style>");
		}

		foreach (var volume in context.Volumes) {
			html.AppendLine($"<div class='volume' id='volume-{volume.Id}'>");
			html.AppendLine($"<h2>{volume.Title}</h2>");

			var orderedChapters = context.ChapterMap.TryGetValue(volume.Id, out var chapters)
				? chapters
				: [];

			foreach (var chapter in orderedChapters) {
				html.AppendLine($"<div class='chapter' id='chapter-{chapter.Id}'>");
				html.AppendLine($"<h3>{chapter.Title}</h3>");
				html.AppendLine("</div>");
			}

			html.AppendLine("</div>");
		}

		html.AppendLine("</div>");
	}
}
