namespace Grimoire.Application.Service.Contract;

using System.Threading;
using Dto.Book;

public interface ISeriesSyncService {
	public Task SyncSeriesTree(Guid seriesId, SyncSeriesRequestDto request, CancellationToken cancellationToken = default);
}
