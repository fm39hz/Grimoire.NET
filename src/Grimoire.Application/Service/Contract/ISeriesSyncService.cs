namespace Grimoire.Application.Service.Contract;

using System.Threading;
using Domain.Entity.Book;
using Dto.Book;

public interface ISeriesSyncService {
	public Task SyncSeriesTree(Guid seriesId, SyncSeriesRequestDto request, CancellationToken cancellationToken = default);
}
