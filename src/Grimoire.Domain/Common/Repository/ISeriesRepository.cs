namespace Grimoire.Domain.Common.Repository;

using System.Threading;
using Entity.Book;

using Microsoft.EntityFrameworkCore;

public interface ISeriesRepository : IRepository<SeriesModel> {
	public Task<SeriesModel?> FindOneByTitle(string title, CancellationToken cancellationToken = default);
	public Task DeleteSubtreeAsync(Guid seriesId, LTree path, CancellationToken cancellationToken = default);
}
