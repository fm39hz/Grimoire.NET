namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;

public sealed class SeriesRepository(ApplicationDbContext context)
	: CrudRepository<SeriesModel>(context), ISeriesRepository {
}
