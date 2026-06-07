namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using System.Threading;
using Microsoft.EntityFrameworkCore;

public sealed class ChapterRepository(ApplicationDbContext context)
	: CrudRepository<ChapterModel>(context), IChapterRepository {
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

	public async Task<ChapterModel?> FindByVolumeIdAndOrder(Guid volumeId, float order, CancellationToken cancellationToken = default) =>
		await Entities
			.AsSplitQuery()
			.Include(c => c.ContentData)
			.FirstOrDefaultAsync(c => c.VolumeId == volumeId && c.Order == order, cancellationToken);
}
