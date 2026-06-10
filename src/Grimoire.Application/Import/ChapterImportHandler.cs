namespace Grimoire.Application.Import;

using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;
using Service.Contract;
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

public sealed class ChapterImportHandler(IChapterService chapterService) : IChapterImportHandler {
 
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
 
        var (_, created) = await chapterService.UpsertAsync(volumeId, dto, cancellationToken);
        return new ChapterImportResult { Created = created };
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
