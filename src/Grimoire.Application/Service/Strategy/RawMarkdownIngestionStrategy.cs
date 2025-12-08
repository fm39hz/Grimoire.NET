namespace Grimoire.Application.Service.Strategy;

using System.Text.RegularExpressions;
using Common;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;

/// <summary>
///     Strategy for ingesting raw Markdown content
/// </summary>
public partial class RawMarkdownIngestionStrategy : IIngestionStrategy {
	[GeneratedRegex(@"<[^>]+>", RegexOptions.Compiled)]
	private static partial Regex HtmlTagRegex();

	public bool CanHandle(CreateChapterRequestDto dto) {
		if (string.IsNullOrWhiteSpace(dto.RawContent)) {
			return false;
		}

		// Check if content contains HTML tags (simple validation)
		return !HtmlTagRegex().IsMatch(dto.RawContent);
	}

	public Task<IngestionResult> ExecuteAsync(CreateChapterRequestDto dto) {
		if (!CanHandle(dto)) {
			throw new InvalidOperationException("This strategy cannot handle the provided DTO");
		}

		var volumeId = PrefixedId.ToGuid(dto.VolumeId);
		var chapterId = Guid.CreateVersion7();
		var sourceId = Guid.CreateVersion7();

		// Parse RawContent into segments (simple split by newline)
		var lines = dto.RawContent!.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		var segments = new List<SegmentModel>();

		foreach (var line in lines) {
			var trimmedLine = line.Trim();
			if (!string.IsNullOrWhiteSpace(trimmedLine)) {
				segments.Add(new TextSegmentModel {
					Id = Guid.CreateVersion7(),
					Runs = [
						new TextRun(trimmedLine)
					]
				});
			}
		}

		// Create SourceMaterial for backup
		// Note: We need to get SeriesId from Volume, but we don't have it in DTO
		// For now, we'll create a placeholder - this should be fetched from the database
		var source = new SourceMaterial {
			Id = sourceId,
			SeriesId = Guid.Empty, // TODO: Fetch from Volume entity
			Title = $"{dto.Title} - Raw Source",
			MarkdownContent = dto.RawContent!
		};

		var chapter = new ChapterModel {
			Id = chapterId,
			VolumeId = volumeId,
			Order = dto.Order,
			Title = dto.Title,
			Status = ChapterStatus.Draft
		};

		var content = new ChapterContentModel {
			Id = chapterId,
			Segments = segments,
			Footnotes = []
		};

		return Task.FromResult(new IngestionResult(chapter, content, source));
	}
}
