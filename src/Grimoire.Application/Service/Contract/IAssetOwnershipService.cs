namespace Grimoire.Application.Service.Contract;

public interface IAssetOwnershipService {
	public Task ReconcileSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default);
}
