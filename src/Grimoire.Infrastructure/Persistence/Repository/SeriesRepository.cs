namespace Grimoire.Infrastructure.Persistence.Repository;

using Application.Dto.Common;
using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;

public sealed class SeriesRepository(ApplicationDbContext context)
	: CrudRepository<SeriesModel>(context), ISeriesRepository {
	
	public async Task<PagedResult<SeriesModel>> FindAllPaged(PaginationRequest request) {
		var query = Entities.AsQueryable();
		var count = await query.CountAsync();

		var items = await query
			.Skip((request.PageIndex - 1) * request.PageSize)
			.Take(request.PageSize)
			.ToListAsync();

		return new PagedResult<SeriesModel>(items, count, request.PageIndex, request.PageSize);
	}
}
