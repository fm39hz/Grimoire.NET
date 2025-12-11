namespace Grimoire.Application.Service.Strategy;

using System.Text.RegularExpressions;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;

/// <summary>
///     Strategy for ingesting raw Markdown content
/// </summary>
public class RawMarkdownIngestionStrategy(IVolumeRepository volumeRepository) : IIngestionStrategy {
	private static Regex HtmlTagRegex { get; } = new("<[^>]+>");

	public bool CanHandle(CreateChapterRequestDto dto) {
		if (string.IsNullOrWhiteSpace(dto.RawContent)) {
			return false;
		}

		// Check if content contains HTML tags (simple validation)
		return !HtmlTagRegex.IsMatch(dto.RawContent);
	}

	public async Task<IngestionResult> ExecuteAsync(CreateChapterRequestDto dto, Guid volumeId) {
		if (!CanHandle(dto)) {
			throw new InvalidOperationException("This strategy cannot handle the provided DTO");
		}

		var chapterId = Guid.CreateVersion7();
		var sourceId = Guid.CreateVersion7();

		// Fetch the volume to get SeriesId
		var volume = await volumeRepository.FindOne(volumeId);
		if (volume == null) {
			throw new InvalidOperationException($"Volume with ID {dto.VolumeId} not found");
		}

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

		// Create SourceMaterial for backup with the correct SeriesId
		var source = new SourceMaterial {
			Id = sourceId,
			SeriesId = volume.SeriesId,
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

		var content = new ChapterContentModel { Id = chapterId, Segments = segments, Footnotes = [] };

		return new IngestionResult(chapter, content, source);
	}
}
