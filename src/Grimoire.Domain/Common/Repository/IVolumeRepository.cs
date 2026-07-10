namespace Grimoire.Domain.Common.Repository;

using System.Threading;
using Entity.Book;
using ValueObject;

public interface IVolumeRepository : IRepository<VolumeModel> {
	public Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, CancellationToken cancellationToken = default);
	public Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
	public Task<int> CountBySeriesId(Guid seriesId, CancellationToken cancellationToken = default);
	public Task<VolumeModel?> FindBySeriesIdAndOrder(Guid seriesId, double order, CancellationToken cancellationToken = default);
	public Task MoveVolumeAsync(Guid volumeId, BookPath oldPath, BookPath newPath, double newOrder, CancellationToken cancellationToken = default);
	public Task DeleteSubtreeAsync(Guid volumeId, BookPath path, CancellationToken cancellationToken = default);
}
