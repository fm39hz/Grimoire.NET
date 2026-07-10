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
			.Where(c => c.DbPath.MatchesLQuery($"{volumePath}.*"))
			.OrderBy(c => c.Order)
			.ToListAsync(cancellationToken);
	}

	public async Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId, int pageIndex, int pageSize, CancellationToken cancellationToken = default) {
		LTree volumePath = "n" + volumeId.ToString("N");
		return await Entities
			.AsNoTracking()
			.Where(c => c.DbPath.MatchesLQuery($"{volumePath}.*"))
			.OrderBy(c => c.Order)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);
	}

	public async Task<int> CountByVolumeId(Guid volumeId, CancellationToken cancellationToken = default) {
		LTree volumePath = "n" + volumeId.ToString("N");
		return await Entities
			.AsNoTracking()
			.Where(c => c.DbPath.MatchesLQuery($"{volumePath}.*"))
			.CountAsync(cancellationToken);
	}

	public async Task<IEnumerable<ChapterModel>> FindByVolumeIds(IEnumerable<Guid> volumeIds, CancellationToken cancellationToken = default) {
		var labels = volumeIds.Select(id => "n" + id.ToString("N")).ToList();
		return await Entities
			.AsNoTracking()
			.Where(c => labels.Contains((string)c.DbPath.Subpath(1, 1)))
			.OrderBy(c => c.Order)
			.ToListAsync(cancellationToken);
	}

	public async Task<IEnumerable<ChapterModel>> FindByVolumeIdsWithContent(IEnumerable<Guid> volumeIds, CancellationToken cancellationToken = default) => await FindByVolumeIds(volumeIds, cancellationToken);

	public async Task<ChapterModel?> FindByVolumeIdAndOrder(Guid volumeId, double order, CancellationToken cancellationToken = default) {
		LTree volumePath = "n" + volumeId.ToString("N");
		return await Entities
			.FirstOrDefaultAsync(c => c.DbPath.MatchesLQuery($"{volumePath}.*") && c.Order == order, cancellationToken);
	}

	public async Task MoveChapterAsync(Guid chapterId, BookPath oldPath, BookPath newPath, double newOrder, CancellationToken cancellationToken = default) {
		var ltreeOld = (LTree)oldPath.Value;
		var ltreeNew = (LTree)newPath.Value;
		var oldPathLength = oldPath.Level;

		await Entities.Where(c => c.Id == chapterId)
			.ExecuteUpdateAsync(s => s.SetProperty(c => c.DbPath, ltreeNew).SetProperty(c => c.Order, newOrder), cancellationToken);

		await context.Segments.Where(seg => seg.DbPath.IsDescendantOf(ltreeOld))
			.ExecuteUpdateAsync(s => s.SetProperty(seg => seg.DbPath, seg => (LTree)((string)ltreeNew + (string)seg.DbPath.Subpath(oldPathLength))), cancellationToken);
	}

	public async Task DeleteSubtreeAsync(Guid chapterId, BookPath path, CancellationToken cancellationToken = default) {
		var ltreePath = (LTree)path.Value;
		await context.Segments.Where(s => s.DbPath.IsDescendantOf(ltreePath)).ExecuteDeleteAsync(cancellationToken);
		await Entities.Where(c => c.Id == chapterId).ExecuteDeleteAsync(cancellationToken);
	}
}
