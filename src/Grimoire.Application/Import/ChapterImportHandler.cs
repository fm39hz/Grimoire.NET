namespace Grimoire.Application.Import;

using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;
using Service.Strategy;
using Microsoft.Extensions.Logging;

public sealed record ChapterImportResult {
    public bool Created { get; init; }
}

public interface IChapterImportHandler {
    Task<ChapterImportResult> ImportAsync(
        Guid volumeId,
        NormalizedChapter chapter,
        Dictionary<string, string> imageFileMap,
        CancellationToken cancellationToken = default);
}

public sealed class ChapterImportHandler(
    IChapterRepository chapterRepository,
    ISourceMaterialRepository sourceRepository,
    IngestionStrategyFactory strategyFactory,
    ILogger<ChapterImportHandler> logger) : IChapterImportHandler {

    public async Task<ChapterImportResult> ImportAsync(
        Guid volumeId,
        NormalizedChapter chapter,
        Dictionary<string, string> imageFileMap,
        CancellationToken cancellationToken = default) {

        var segments = RemapImages(chapter.Segments, imageFileMap);

        var dto = new CreateChapterRequestDto(
            PrefixedId.ToString(EntityPrefix.Volume, volumeId),
            chapter.Order,
            chapter.Title,
            segments,
            chapter.Footnotes,
            null);

        var strategy = strategyFactory.GetStrategy(dto);
        var ingest = await strategy.ExecuteAsync(dto, volumeId, cancellationToken);

        var existing = await chapterRepository.FindByVolumeIdAndOrder(volumeId, chapter.Order, cancellationToken);
        if (existing is not null)
        {
            existing.Title = ingest.Chapter.Title;
            existing.Status = ingest.Chapter.Status;
            if (existing.ContentData != null)
            {
                existing.ContentData.Segments = ingest.Content.Segments;
                existing.ContentData.Footnotes = ingest.Content.Footnotes;
            }
            else
            {
                existing.ContentData = new ChapterContentModel
                {
                    Id = existing.Id,
                    Segments = ingest.Content.Segments,
                    Footnotes = ingest.Content.Footnotes
                };
            }
            if (ingest.Source is not null)
                await sourceRepository.Create(ingest.Source, cancellationToken);
            await chapterRepository.Update(existing, cancellationToken);
            return new ChapterImportResult { Created = false };
        }

        if (ingest.Source is not null)
            await sourceRepository.Create(ingest.Source, cancellationToken);
        ingest.Chapter.ContentData = ingest.Content;
        await chapterRepository.Create(ingest.Chapter, cancellationToken);
        return new ChapterImportResult { Created = true };
    }

    private static List<SegmentModel> RemapImages(
        List<SegmentModel> segments,
        Dictionary<string, string> assetMap) {

        return segments.Select(s =>
        {
            if (s is ImageSegmentModel img && assetMap.TryGetValue(img.AssetKey, out var key))
                return img with { AssetKey = key };
            return s;
        }).ToList();
    }
}
