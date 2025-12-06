namespace Grimoire.Domain.Common.Repository;

using Entity.Book;

public interface IVolumeRepository : IRepository<VolumeModel> {
	public Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId);
	public Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, int pageIndex, int pageSize);
	public Task<int> CountBySeriesId(Guid seriesId);
}
