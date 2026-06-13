namespace Grimoire.Application.Persistence;

using System.Threading;
using System.Threading.Tasks;
using Domain.Common;
using Dto.Book;

public interface IVolumeProjectedQuery {
	Task<PagedResult<VolumeResponseDto>> FindAllProjectedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}
