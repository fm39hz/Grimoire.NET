namespace Grimoire.Domain.Common.Repository;

using Entity.Book;

public interface ISeriesRepository : IRepository<SeriesModel> {
	public Task<SeriesModel?> GetAsync(string slug);
}
