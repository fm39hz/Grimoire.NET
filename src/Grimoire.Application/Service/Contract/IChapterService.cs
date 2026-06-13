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

	// TODO: Implement Postgres-native Full-Text Search (FTS) on chapter text content.
	// We can leverage:
	// - EF.Functions.ToTsVector("english", content) / ToTsVector(document) to build search vectors
	// - EF.Functions.WebSearchToTsQuery("english", query) to parse user inputs (supporting quotes, OR, AND operators)
	// - EF.Functions.Matches(vector, query) to perform GIN-indexed matches
	// - EF.Functions.GetResultHeadline(query, document) to generate HTML-highlighted snippets
	// - EF.Functions.Rank(vector, query) to sort matching results by relevance
	// Task<IEnumerable<ChapterModel>> SearchChaptersAsync(string query, CancellationToken cancellationToken = default);
}
