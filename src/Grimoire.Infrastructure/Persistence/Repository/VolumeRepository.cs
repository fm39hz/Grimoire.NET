namespace Grimoire.Infrastructure.Persistence.Repository;

using System.Threading;
using Database;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Common.ValueObject;
using Domain.Entity.Book;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Mapper;
using Grimoire.Application.Persistence;
using Microsoft.EntityFrameworkCore;

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

	public async Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, CancellationToken cancellationToken = default) {
		LTree seriesPath = "n" + seriesId.ToString("N");
		return await Entities
			.AsNoTracking()
			.Where(v => v.DbPath.MatchesLQuery($"{seriesPath}.*"))
			.OrderBy(v => v.Order)
			.ToListAsync(cancellationToken);
	}

	public async Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, int pageIndex, int pageSize, CancellationToken cancellationToken = default) {
		LTree seriesPath = "n" + seriesId.ToString("N");
		return await Entities
			.AsNoTracking()
			.Where(v => v.DbPath.MatchesLQuery($"{seriesPath}.*"))
			.OrderBy(v => v.Order)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);
	}

	public async Task<int> CountBySeriesId(Guid seriesId, CancellationToken cancellationToken = default) {
		LTree seriesPath = "n" + seriesId.ToString("N");
		return await Entities
			.AsNoTracking()
			.Where(v => v.DbPath.MatchesLQuery($"{seriesPath}.*"))
			.CountAsync(cancellationToken);
	}

	public async Task<VolumeModel?> FindBySeriesIdAndOrder(Guid seriesId, double order, CancellationToken cancellationToken = default) {
		LTree seriesPath = "n" + seriesId.ToString("N");
		return await Entities
			.FirstOrDefaultAsync(v => v.DbPath.MatchesLQuery($"{seriesPath}.*") && v.Order == order, cancellationToken);
	}

	public async Task MoveVolumeAsync(Guid volumeId, BookPath oldPath, BookPath newPath, double newOrder, CancellationToken cancellationToken = default) {
		var ltreeOld = (LTree)oldPath.Value;
		var ltreeNew = (LTree)newPath.Value;
		var oldPathLength = oldPath.Level;

		await Entities.Where(v => v.Id == volumeId)
			.ExecuteUpdateAsync(s => s.SetProperty(v => v.DbPath, ltreeNew).SetProperty(v => v.Order, newOrder), cancellationToken);

		await context.Chapters.Where(c => c.DbPath.IsDescendantOf(ltreeOld))
			.ExecuteUpdateAsync(s => s.SetProperty(c => c.DbPath, c => (LTree)((string)ltreeNew + (string)c.DbPath.Subpath(oldPathLength))), cancellationToken);

		await context.Segments.Where(seg => seg.DbPath.IsDescendantOf(ltreeOld))
			.ExecuteUpdateAsync(s => s.SetProperty(seg => seg.DbPath, seg => (LTree)((string)ltreeNew + (string)seg.DbPath.Subpath(oldPathLength))), cancellationToken);
	}

	public async Task DeleteSubtreeAsync(Guid volumeId, BookPath path, CancellationToken cancellationToken = default) {
		var ltreePath = (LTree)path.Value;
		await context.Segments.Where(s => s.DbPath.IsDescendantOf(ltreePath)).ExecuteDeleteAsync(cancellationToken);
		await context.Chapters.Where(c => c.DbPath.IsDescendantOf(ltreePath)).ExecuteDeleteAsync(cancellationToken);
		await Entities.Where(v => v.Id == volumeId).ExecuteDeleteAsync(cancellationToken);
	}
}
