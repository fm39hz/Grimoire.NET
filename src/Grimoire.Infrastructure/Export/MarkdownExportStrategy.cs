namespace Grimoire.Infrastructure.Export;

using System.Text;
using Application.Common;
using Application.Dto.Book;
using Application.Export;
using Application.Service.Strategy;
using Common;
using Domain.Entity.Book;

/// <summary>
///     Strategy for exporting series to Markdown format
/// </summary>
public class MarkdownExportStrategy : IExportStrategy {
	public ExportFormat Format => ExportFormat.Markdown;

	public async Task<ExportResult> ExportAsync(BookExportContext context) {
		try {
			var markdown = new StringBuilder();

			// Process structure sections
			foreach (var section in context.Structure.Sections) {
				ProcessSection(section, context, markdown);
			}

			var memoryStream = new MemoryStream();
			await using var writer = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true);
			await writer.WriteAsync(markdown.ToString());
			await writer.FlushAsync();
			memoryStream.Position = 0;

			var fileName = $"{ExportUtilities.SanitizeFileName(context.Series.Title)}.md";

			return ExportResult.Ok(memoryStream, fileName, "text/markdown");
		}
		catch (Exception ex) {
			return ExportResult.Fail(ex.Message);
		}
	}

	private static void ProcessSection(ExportSectionDto section, BookExportContext context, StringBuilder markdown) {
		switch (section.Type) {
			case BookSection.IntroPage:
			case BookSection.Intro:
				ProcessIntroSection(section, context.Series, markdown);
				break;

			case BookSection.Toc:
			case BookSection.TableOfContents:
				ProcessTocSection(context, markdown);
				break;

			case BookSection.Content:
			case BookSection.Chapters:
				ProcessContentSection(context, markdown);
				break;

			case BookSection.Description:
				ProcessDescriptionSection(section, context.Series, markdown);
				break;

			case BookSection.Unknown:
			default:
				break;
		}
	}

	private static void ProcessIntroSection(ExportSectionDto? section, SeriesModel series, StringBuilder markdown) {
		var splitDescription = ExportUtilities.IsSplitDescriptionEnabled(section);

		// Title
		markdown.AppendLine($"# {series.Title}");
		markdown.AppendLine();

		// Authors
		if (series.Metadata.Authors.Count > 0) {
			markdown.AppendLine($"**Author:** {string.Join(", ", series.Metadata.Authors)}");
			markdown.AppendLine();
		}

		// Artists
		if (series.Metadata.Artists.Count > 0) {
			markdown.AppendLine($"**Artist:** {string.Join(", ", series.Metadata.Artists)}");
			markdown.AppendLine();
		}

		// Description (if not split)
		if (!splitDescription && series.Metadata.Description.Count > 0) {
			markdown.AppendLine("## Description");
			markdown.AppendLine();
			var descriptionMarkdown = SegmentMarkdownConverter.ConvertTextSegmentsToMarkdown(series.Metadata.Description);
			markdown.AppendLine(descriptionMarkdown);
			markdown.AppendLine();
		}

		// Tags
		if (series.Metadata.Tags.Count > 0) {
			markdown.AppendLine($"**Tags:** {string.Join(", ", series.Metadata.Tags)}");
			markdown.AppendLine();
		}

		markdown.AppendLine("---");
		markdown.AppendLine();
	}

	private static void ProcessDescriptionSection(ExportSectionDto? section, SeriesModel series, StringBuilder markdown) {
		var splitDescription = ExportUtilities.IsSplitDescriptionEnabled(section);
		if (splitDescription || series.Metadata.Description.Count == 0) {
			return;
		}

		markdown.AppendLine("## Description");
		markdown.AppendLine();
		var descriptionMarkdown = SegmentMarkdownConverter.ConvertTextSegmentsToMarkdown(series.Metadata.Description);
		markdown.AppendLine(descriptionMarkdown);
		markdown.AppendLine();
		markdown.AppendLine("---");
		markdown.AppendLine();
	}

	private static void ProcessTocSection(BookExportContext context, StringBuilder markdown) {
		markdown.AppendLine("## Table of Contents");
		markdown.AppendLine();

		foreach (var volume in context.Volumes) {
			markdown.AppendLine($"### {volume.Title}");
			markdown.AppendLine();

			if (context.ChapterMap.TryGetValue(volume.Id, out var chapters)) {
				foreach (var chapter in chapters) {
					// Use anchor link format for internal navigation
					var anchor = GenerateAnchor(chapter.Title);
					markdown.AppendLine($"- [{chapter.Title}](#{anchor})");
				}
			}

			markdown.AppendLine();
		}

		markdown.AppendLine("---");
		markdown.AppendLine();
	}

	private static void ProcessContentSection(BookExportContext context, StringBuilder markdown) {
		foreach (var volume in context.Volumes) {
			markdown.AppendLine($"## {volume.Title}");
			markdown.AppendLine();

			if (!context.ChapterMap.TryGetValue(volume.Id, out var chapters)) {
				continue;
			}

			foreach (var chapter in chapters) {
				// Chapter title with anchor
				markdown.AppendLine($"### {chapter.Title}");
				markdown.AppendLine();

				// Chapter content
				if (chapter.ContentData is not null && chapter.ContentData.Segments.Count > 0) {
					var chapterMarkdown = SegmentMarkdownConverter.ConvertToMarkdown(
						chapter.ContentData.Segments,
						chapter.ContentData.Footnotes
					);
					markdown.AppendLine(chapterMarkdown);
				}

				markdown.AppendLine();
				markdown.AppendLine("---");
				markdown.AppendLine();
			}
		}
	}

	/// <summary>
	///     Generates a URL-safe anchor from a title (for internal linking in TOC)
	/// </summary>
	private static string GenerateAnchor(string title) {
		return title
			.ToLowerInvariant()
			.Replace(" ", "-")
			.Replace("'", "")
			.Replace("\"", "")
			.Replace(".", "")
			.Replace(",", "")
			.Replace("!", "")
			.Replace("?", "");
	}
}
