namespace Grimoire.Domain.Common.Repository;

using System.Threading;
using Entity.Book;

public interface ISeriesRepository : IRepository<SeriesModel> {
	public Task<SeriesModel?> FindOneByTitle(string title, CancellationToken cancellationToken = default);
}
