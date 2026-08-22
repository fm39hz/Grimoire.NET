namespace Grimoire.Application.Service.Strategy;

using System.Threading;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Dto.Book;

/// <summary>
///     Strategy for ingesting raw Markdown content.
///     Parsing is delegated to <see cref="MarkdownSegmentParser"/>, which maps paragraphs,
///     inline formatting, images, dividers, footnotes and tables onto typed segments and
///     preserves every other construct verbatim inside text segments.
/// </summary>
public class RawMarkdownIngestionStrategy(MarkdownSegmentParser parser, IVolumeRepository volumeRepository) : IIngestionStrategy {

	public bool CanHandle(CreateChapterRequestDto dto) => !string.IsNullOrWhiteSpace(dto.RawContent);

	public async Task<IngestionResult> ExecuteAsync(CreateChapterRequestDto dto, Guid volumeId, CancellationToken cancellationToken = default) {
		if (!CanHandle(dto)) {
			throw new InvalidOperationException("This strategy cannot handle the provided DTO");
		}

		var chapterId = Guid.CreateVersion7();
		var sourceId = Guid.CreateVersion7();

		// Fetch the volume to get SeriesId via Path
		var volume = await volumeRepository.FindOne(volumeId, cancellationToken) ??
					throw new InvalidOperationException($"Volume with ID {volumeId} not found");

		var seriesId = volume.Path.GetSeriesId();

		var parsed = parser.Parse(dto.RawContent!);

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

		return new IngestionResult(chapter, parsed.Segments, source);
	}
}
