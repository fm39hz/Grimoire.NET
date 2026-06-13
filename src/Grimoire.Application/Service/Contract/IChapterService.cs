namespace Grimoire.Application.Service.Contract;

using System.Threading;
using Domain.Entity.Book;
using Dto.Book;

public interface IChapterService : ICrudService<ChapterModel, CreateChapterRequestDto, UpdateChapterRequestDto> {
	public Task<(ChapterModel Chapter, bool Created)> UpsertAsync(Guid volumeId, CreateChapterRequestDto dto, CancellationToken cancellationToken = default);
	public Task<(ChapterModel Chapter, bool Created)> UpsertAsync(Guid volumeId, CreateChapterRequestDto dto, ChapterModel? existing, CancellationToken cancellationToken = default);
	public Task<(IEnumerable<ChapterModel> Chapters, int CreatedCount, int UpdatedCount)> UpsertBulkAsync(
		Guid seriesId,
		System.Collections.Generic.List<(Guid VolumeId, CreateChapterRequestDto Dto)> chapters,
		System.Action<int>? onProgress = null,
		CancellationToken cancellationToken = default);
	public Task<IEnumerable<ChapterModel>> SplitAsync(Guid chapterId, SplitChapterRequestDto dto, CancellationToken cancellationToken = default);
	public Task<ChapterModel> MergeAsync(MergeChaptersRequestDto dto, CancellationToken cancellationToken = default);

	// TODO: Implement Postgres-native Full-Text Search (FTS) using tsvector / tsquery GIN index on chapter segments text content.
	// Task<IEnumerable<ChapterModel>> SearchChaptersAsync(string query, CancellationToken cancellationToken = default);
}
