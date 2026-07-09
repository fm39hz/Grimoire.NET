namespace Grimoire.Application.Service.Strategy;

using System.Threading;
using System.Text.RegularExpressions;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;
using Grimoire.Domain.Common.Extensions;

/// <summary>
///     Strategy for ingesting raw Markdown content
/// </summary>
public partial class RawMarkdownIngestionStrategy(IVolumeRepository volumeRepository) : IIngestionStrategy {
	[GeneratedRegex("<[^>]+>")] private static partial Regex HtmlTagRegex { get; }

	public bool CanHandle(CreateChapterRequestDto dto) {
		if (string.IsNullOrWhiteSpace(dto.RawContent)) {
			return false;
		}

		// Check if content contains HTML tags (simple validation)
		return !HtmlTagRegex.IsMatch(dto.RawContent);
	}

	public async Task<IngestionResult> ExecuteAsync(CreateChapterRequestDto dto, Guid volumeId, CancellationToken cancellationToken = default) {
		if (!CanHandle(dto)) {
			throw new InvalidOperationException("This strategy cannot handle the provided DTO");
		}

		var chapterId = Guid.CreateVersion7();
		var sourceId = Guid.CreateVersion7();

		// Fetch the volume to get SeriesId via Path
		var volume = await volumeRepository.FindOne(volumeId, cancellationToken) ??
					throw new InvalidOperationException($"Volume with ID {volumeId} not found");

		Guid seriesId = volume.Path.GetSeriesId();

		// Parse RawContent into segments (simple split by newline)
		var lines = dto.RawContent!.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		var segments = new List<SegmentModel>();
		double order = 1.0;

		foreach (var line in lines) {
			var trimmedLine = line.Trim();
			if (!string.IsNullOrWhiteSpace(trimmedLine)) {
				segments.Add(new TextSegmentModel {
					Id = Guid.CreateVersion7(),
					Runs = [
						new TextRun(trimmedLine)
					],
					Order = order++
				});
			}
		}

		// Create SourceMaterial for backup with the correct SeriesId
		var source = new SourceMaterial {
			Id = sourceId,
			SeriesId = seriesId,
			Title = $"{dto.Title} - Raw Source",
			MarkdownContent = dto.RawContent!
		};

		var chapter = new ChapterModel {
			Id = chapterId,
			Order = dto.Order,
			Title = dto.Title,
			Status = ChapterStatus.Draft
		};

		return new IngestionResult(chapter, segments, source);
	}
}
