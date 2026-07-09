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

public sealed class ChapterRepository(ApplicationDbContext context, IBookMapper mapper)
	: CrudRepository<ChapterModel>(context), IChapterRepository, IChapterProjectedQuery {
	public async Task<PagedResult<ChapterListResponseDto>> FindAllProjectedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default) {
		var query = Entities.AsNoTracking().OrderBy(c => c.Id);
		var count = await query.CountAsync(cancellationToken);
		var items = await mapper.ProjectToChapterListDto(query)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);

		return new PagedResult<ChapterListResponseDto>(items, count, pageIndex, pageSize);
	}
	public async Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId, CancellationToken cancellationToken = default) {
		LTree volumePath = "n" + volumeId.ToString("N");
		return await Entities
			.AsNoTracking()
			.Where(c => c.Path.MatchesLQuery($"{volumePath}.*"))
			.OrderBy(c => c.Order)
			.ToListAsync(cancellationToken);
	}

	public async Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId, int pageIndex, int pageSize, CancellationToken cancellationToken = default) {
		LTree volumePath = "n" + volumeId.ToString("N");
		return await Entities
			.AsNoTracking()
			.Where(c => c.Path.MatchesLQuery($"{volumePath}.*"))
			.OrderBy(c => c.Order)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);
	}

	public async Task<int> CountByVolumeId(Guid volumeId, CancellationToken cancellationToken = default) {
		LTree volumePath = "n" + volumeId.ToString("N");
		return await Entities
			.AsNoTracking()
			.Where(c => c.Path.MatchesLQuery($"{volumePath}.*"))
			.CountAsync(cancellationToken);
	}

	public async Task<IEnumerable<ChapterModel>> FindByVolumeIds(IEnumerable<Guid> volumeIds, CancellationToken cancellationToken = default) {
		var volGuidStrings = volumeIds.Select(id => id.ToString("N")).ToList();
		return await Entities
			.AsNoTracking()
			.Where(c => volGuidStrings.Contains(c.Path.ToString().Substring(35, 32)))
			.OrderBy(c => c.Order)
			.ToListAsync(cancellationToken);
	}

	public async Task<IEnumerable<ChapterModel>> FindByVolumeIdsWithContent(IEnumerable<Guid> volumeIds, CancellationToken cancellationToken = default) {
		// Content is now loaded separately from Segments table, this method just returns chapters.
		return await FindByVolumeIds(volumeIds, cancellationToken);
	}

	public async Task<ChapterModel?> FindByVolumeIdAndOrder(Guid volumeId, double order, CancellationToken cancellationToken = default) {
		LTree volumePath = "n" + volumeId.ToString("N");
		return await Entities
			.FirstOrDefaultAsync(c => c.Path.MatchesLQuery($"{volumePath}.*") && c.Order == order, cancellationToken);
	}

	public async Task MoveChapterAsync(Guid chapterId, LTree oldPath, LTree newPath, double newOrder, CancellationToken cancellationToken = default) {
		var oldPathLength = oldPath.ToString().Split('.').Length;

		await Entities.Where(c => c.Id == chapterId)
			.ExecuteUpdateAsync(s => s.SetProperty(c => c.Path, newPath).SetProperty(c => c.Order, newOrder), cancellationToken);

		await context.Segments.Where(seg => seg.Path.IsDescendantOf(oldPath))
			.ExecuteUpdateAsync(s => s.SetProperty(seg => seg.Path, seg => (LTree)((string)newPath + (string)seg.Path.Subpath(oldPathLength))), cancellationToken);
	}

	public async Task DeleteSubtreeAsync(Guid chapterId, LTree path, CancellationToken cancellationToken = default) {
		await context.Segments.Where(s => s.Path.IsDescendantOf(path)).ExecuteDeleteAsync(cancellationToken);
		await Entities.Where(c => c.Id == chapterId).ExecuteDeleteAsync(cancellationToken);
	}
}
