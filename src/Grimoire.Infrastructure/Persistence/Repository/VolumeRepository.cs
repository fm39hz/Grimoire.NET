namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Mapper;
using Grimoire.Application.Persistence;

public sealed class VolumeRepository(ApplicationDbContext context, IBookMapper mapper)
	: CrudRepository<VolumeModel>(context), IVolumeRepository, IVolumeProjectedQuery {
	public async Task<PagedResult<VolumeResponseDto>> FindAllProjectedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default) {
		var query = Entities.AsNoTracking().OrderBy(v => v.Id);
		var count = await query.CountAsync(cancellationToken);
		var items = await mapper.ProjectToVolumeDto(query)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);

		return new PagedResult<VolumeResponseDto>(items, count, pageIndex, pageSize);
	}
	public async Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.Where(v => v.SeriesId == seriesId)
			.OrderBy(v => v.Order)
			.ToListAsync(cancellationToken);

	public async Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, int pageIndex, int pageSize, CancellationToken cancellationToken = default) {
		var items = await Entities
			.AsNoTracking()
			.Where(v => v.SeriesId == seriesId)
			.OrderBy(v => v.Order)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);

		return items;
	}

	public async Task<int> CountBySeriesId(Guid seriesId, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.Where(v => v.SeriesId == seriesId)
			.CountAsync(cancellationToken);

	public async Task<VolumeModel?> FindBySeriesIdAndOrder(Guid seriesId, double order, CancellationToken cancellationToken = default) =>
		await Entities
			.FirstOrDefaultAsync(v => v.SeriesId == seriesId && v.Order == order, cancellationToken);
}
