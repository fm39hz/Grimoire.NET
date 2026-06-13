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
	public override async Task<ChapterModel?> FindOne(Guid id, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.AsSplitQuery()
			.Include(c => c.ContentData)
			.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

	public async Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.AsSplitQuery()
			.Where(c => c.VolumeId == volumeId)
			.OrderBy(c => c.Order)
			.ToListAsync(cancellationToken);

	public async Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId, int pageIndex, int pageSize, CancellationToken cancellationToken = default) {
		var items = await Entities
			.AsNoTracking()
			.AsSplitQuery()
			.Where(c => c.VolumeId == volumeId)
			.OrderBy(c => c.Order)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);

		return items;
	}

	public async Task<int> CountByVolumeId(Guid volumeId, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.Where(c => c.VolumeId == volumeId)
			.CountAsync(cancellationToken);

	public async Task<IEnumerable<ChapterModel>> FindByVolumeIds(IEnumerable<Guid> volumeIds, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.Where(c => volumeIds.Contains(c.VolumeId))
			.OrderBy(c => c.VolumeId)
			.ThenBy(c => c.Order)
			.ToListAsync(cancellationToken);

	public async Task<IEnumerable<ChapterModel>> FindByVolumeIdsWithContent(IEnumerable<Guid> volumeIds, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.AsSplitQuery()
			.Include(c => c.ContentData)
			.Where(c => volumeIds.Contains(c.VolumeId))
			.OrderBy(c => c.VolumeId)
			.ThenBy(c => c.Order)
			.ToListAsync(cancellationToken);

	public async Task<ChapterModel?> FindByVolumeIdAndOrder(Guid volumeId, double order, CancellationToken cancellationToken = default) =>
		await Entities
			.AsSplitQuery()
			.Include(c => c.ContentData)
			.FirstOrDefaultAsync(c => c.VolumeId == volumeId && c.Order == order, cancellationToken);
}
