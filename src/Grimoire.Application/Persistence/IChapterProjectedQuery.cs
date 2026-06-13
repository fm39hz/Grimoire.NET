namespace Grimoire.Application.Persistence;

using System.Threading;
using System.Threading.Tasks;
using Domain.Common;
using Dto.Book;

public interface IChapterProjectedQuery {
	Task<PagedResult<ChapterListResponseDto>> FindAllProjectedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}
