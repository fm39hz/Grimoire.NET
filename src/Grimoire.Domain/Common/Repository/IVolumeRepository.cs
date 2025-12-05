namespace Grimoire.Domain.Common.Repository;

using Entity.Book;

public interface IVolumeRepository : IRepository<VolumeModel> {
	public Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId);
}
