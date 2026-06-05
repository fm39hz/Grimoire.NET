namespace Grimoire.Domain.Common.Repository;

using Entity.Book;

public interface ISeriesRepository : IRepository<SeriesModel> {
	public Task<SeriesModel?> FindOneByTitle(string title);
}
