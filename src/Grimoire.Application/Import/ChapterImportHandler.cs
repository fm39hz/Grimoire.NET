namespace Grimoire.Application.Import;

using Domain.Common;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;
using Mapper;
using Service.Contract;

public sealed record ChapterImportResult {
	public bool Created { get; init; }
}

public interface IChapterImportHandler {
	public Task<ChapterImportResult> ImportAsync(
		Guid volumeId,
		NormalizedChapter chapter,
		Dictionary<string, string> imageFileMap,
		CancellationToken cancellationToken = default);
}

public sealed class ChapterImportHandler(IChapterService chapterService, IBookMapper mapper) : IChapterImportHandler {

	public async Task<ChapterImportResult> ImportAsync(
		Guid volumeId,
		NormalizedChapter chapter,
		Dictionary<string, string> imageFileMap,
		CancellationToken cancellationToken = default) {

		var segments = RemapImages(chapter.Segments, imageFileMap)
			.Select(mapper.ToSegmentDto)
			.ToList();

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
		Dictionary<string, string> assetMap) => [.. segments.Select(s => {
			if (s is ImageSegmentModel img && assetMap.TryGetValue(img.AssetKey, out var key)) {
				return new ImageSegmentModel { Id = img.Id, AssetKey = key };
			}

			return s;
		})];
}
