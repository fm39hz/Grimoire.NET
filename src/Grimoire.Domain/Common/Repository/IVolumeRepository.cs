namespace Grimoire.Domain.Common.Repository;

using Entity.Book;

public interface IVolumeRepository : IRepository<VolumeModel> {
	public Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId);
	public Task<PagedResult<VolumeModel>> FindBySeriesId(Guid seriesId, int pageIndex, int pageSize);
}
