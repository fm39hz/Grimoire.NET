namespace Grimoire.Domain.Common.Repository;

using System.Threading;
using Entity.Book;

public interface IVolumeRepository : IRepository<VolumeModel> {
	public Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, CancellationToken cancellationToken = default);
	public Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
	public Task<int> CountBySeriesId(Guid seriesId, CancellationToken cancellationToken = default);
}
