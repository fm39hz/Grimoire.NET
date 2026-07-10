namespace Grimoire.Domain.Common.Repository;

using System.Threading;
using Entity.Book;
using ValueObject;

public interface ISeriesRepository : IRepository<SeriesModel> {
	public Task<SeriesModel?> FindOneByTitle(string title, CancellationToken cancellationToken = default);
	public Task DeleteSubtreeAsync(Guid seriesId, BookPath path, CancellationToken cancellationToken = default);
}
