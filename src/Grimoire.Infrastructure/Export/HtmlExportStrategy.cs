namespace Grimoire.Infrastructure.Export;

using System.Text;
using Application.Dto.Book;
using Application.Service.Contract;
using Application.Service.Strategy;
using Common;
using Domain.Entity.Book;

/// <summary>
///     Strategy for exporting series to HTML format
/// </summary>
public class HtmlExportStrategy(
	IVolumeService volumeService,
	IChapterService chapterService) : IExportStrategy {
	public ExportFormat Format => ExportFormat.Html;

	public async Task<ExportResult> ExportAsync(
		SeriesModel series,
		IEnumerable<VolumeModel> volumes,
		BinderyRequestDto request) {
		try {
			var volumeList = volumes.ToList();
			var html = new StringBuilder();

			html.AppendLine("<!DOCTYPE html>");
			html.AppendLine("<html>");
			html.AppendLine("<head>");
			html.AppendLine($"<title>{series.Title}</title>");

			// Add CSS
			if (request.Structure?.GlobalCss != null) {
				html.AppendLine("<style>");
				html.AppendLine(request.Structure.GlobalCss);
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

			// Process structure sections (sequential - appending to single StringBuilder)
			// Note: Cannot parallelize as all sections write to the same StringBuilder
			foreach (var section in request.Structure.Sections) {
				await ProcessSection(section, series, volumeList, html);
			}

			html.AppendLine("</body>");
			html.AppendLine("</html>");

			var memoryStream = new MemoryStream();
			await using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
			await writer.WriteAsync(html.ToString());
			await writer.FlushAsync();
			memoryStream.Position = 0;

			var fileName = $"{ExportUtilities.SanitizeFileName(series.Title)}.html";

			return new ExportResult {
				ContentStream = memoryStream, FileName = fileName, ContentType = "text/html", Success = true
			};
		}
		catch (Exception ex) {
			return new ExportResult {
				ContentStream = Stream.Null,
				FileName = string.Empty,
				ContentType = string.Empty,
				Success = false,
				ErrorMessage = ex.Message
			};
		}
	}

	private async Task ProcessSection(ExportSectionDto section, SeriesModel series,
		List<VolumeModel> volumeList, StringBuilder html) {
		switch (section.Type.ToLowerInvariant()) {
			case "intropage":
			case "intro":
				await ProcessIntroSection(section, series, html);
				break;

			case "toc":
			case "tableofcontents":
				await ProcessTocSection(section, volumeList, html);
				break;

			case "content":
			case "chapters":
				await ProcessContentSection(section, volumeList, html);
				break;

			case "description":
				await ProcessDescriptionSection(section, series, html);
				break;
		}
	}

	private Task ProcessIntroSection(ExportSectionDto? section, SeriesModel series, StringBuilder html) {
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

		return Task.CompletedTask;
	}

	private Task ProcessDescriptionSection(ExportSectionDto? section, SeriesModel series, StringBuilder html) {
		var splitDescription = ExportUtilities.IsSplitDescriptionEnabled(section);
		if (splitDescription || series.Metadata?.Description == null || series.Metadata.Description.Count == 0) {
			return Task.CompletedTask;
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

		return Task.CompletedTask;
	}

	private Task ProcessTocSection(ExportSectionDto? section, List<VolumeModel> volumeList, StringBuilder html) {
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

		return Task.CompletedTask;
	}

	private async Task ProcessContentSection(ExportSectionDto? section, List<VolumeModel> volumeList,
		StringBuilder html) {
		html.AppendLine("<div class='section content' id='content'>");

		if (section?.CustomCss != null) {
			html.AppendLine($"<style>{section.CustomCss}</style>");
		}

		foreach (var volume in volumeList) {
			html.AppendLine($"<div class='volume' id='volume-{volume.Id}'>");
			html.AppendLine($"<h2>{volume.Title}</h2>");

			var chapters = await volumeService.FindAllChapters(volume.Id);
			var orderedChapters = chapters.OrderBy(c => c.Order).ToList();

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
