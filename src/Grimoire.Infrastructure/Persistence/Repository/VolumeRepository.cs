namespace Grimoire.Infrastructure.Persistence.Repository;

using Application.Dto.Common;
using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;

public sealed class VolumeRepository(ApplicationDbContext context)
	: CrudRepository<VolumeModel>(context), IVolumeRepository {
	public async Task<PagedResult<VolumeModel>> FindAllPaged(PaginationRequest request) {
		var query = Entities.AsQueryable();
		var count = await query.CountAsync();

		var items = await query
			.Skip((request.PageIndex - 1) * request.PageSize)
			.Take(request.PageSize)
			.ToListAsync();

		return new PagedResult<VolumeModel>(items, count, request.PageIndex, request.PageSize);
	}

	public async Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId) =>
		await Entities
			.Where(v => v.SeriesId == seriesId)
			.OrderBy(v => v.Order)
			.ToListAsync();
}
