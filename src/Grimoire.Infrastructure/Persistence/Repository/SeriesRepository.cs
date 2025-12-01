namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;

public sealed class SeriesRepository(ApplicationDbContext context)
	: CrudRepository<SeriesModel>(context), ISeriesRepository {
	public async Task<SeriesModel?> GetAsync(string slug) =>
		await Entities.FirstOrDefaultAsync(s => s.Title == slug);
}
