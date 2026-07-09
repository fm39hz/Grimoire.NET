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
	public async Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, CancellationToken cancellationToken = default) {
		LTree seriesPath = "n" + seriesId.ToString("N");
		return await Entities
			.AsNoTracking()
			.Where(v => v.Path.MatchesLQuery($"{seriesPath}.*"))
			.OrderBy(v => v.Order)
			.ToListAsync(cancellationToken);
	}

	public async Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, int pageIndex, int pageSize, CancellationToken cancellationToken = default) {
		LTree seriesPath = "n" + seriesId.ToString("N");
		return await Entities
			.AsNoTracking()
			.Where(v => v.Path.MatchesLQuery($"{seriesPath}.*"))
			.OrderBy(v => v.Order)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);
	}

	public async Task<int> CountBySeriesId(Guid seriesId, CancellationToken cancellationToken = default) {
		LTree seriesPath = "n" + seriesId.ToString("N");
		return await Entities
			.AsNoTracking()
			.Where(v => v.Path.MatchesLQuery($"{seriesPath}.*"))
			.CountAsync(cancellationToken);
	}

	public async Task<VolumeModel?> FindBySeriesIdAndOrder(Guid seriesId, double order, CancellationToken cancellationToken = default) {
		LTree seriesPath = "n" + seriesId.ToString("N");
		return await Entities
			.FirstOrDefaultAsync(v => v.Path.MatchesLQuery($"{seriesPath}.*") && v.Order == order, cancellationToken);
	}

	public async Task MoveVolumeAsync(Guid volumeId, LTree oldPath, LTree newPath, double newOrder, CancellationToken cancellationToken = default) {
		var oldPathLength = oldPath.ToString().Split('.').Length;

		await Entities.Where(v => v.Id == volumeId)
			.ExecuteUpdateAsync(s => s.SetProperty(v => v.Path, newPath).SetProperty(v => v.Order, newOrder), cancellationToken);

		await context.Chapters.Where(c => c.Path.IsDescendantOf(oldPath))
			.ExecuteUpdateAsync(s => s.SetProperty(c => c.Path, c => (LTree)((string)newPath + (string)c.Path.Subpath(oldPathLength))), cancellationToken);
			
		await context.Segments.Where(seg => seg.Path.IsDescendantOf(oldPath))
			.ExecuteUpdateAsync(s => s.SetProperty(seg => seg.Path, seg => (LTree)((string)newPath + (string)seg.Path.Subpath(oldPathLength))), cancellationToken);
	}

	public async Task DeleteSubtreeAsync(Guid volumeId, LTree path, CancellationToken cancellationToken = default) {
		await context.Segments.Where(s => s.Path.IsDescendantOf(path)).ExecuteDeleteAsync(cancellationToken);
		await context.Chapters.Where(c => c.Path.IsDescendantOf(path)).ExecuteDeleteAsync(cancellationToken);
		await Entities.Where(v => v.Id == volumeId).ExecuteDeleteAsync(cancellationToken);
	}
}
