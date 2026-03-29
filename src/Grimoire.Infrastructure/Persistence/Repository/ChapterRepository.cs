namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;

public sealed class ChapterRepository(ApplicationDbContext context)
	: CrudRepository<ChapterModel>(context), IChapterRepository {
	public override async Task<ChapterModel?> FindOne(Guid id) =>
		await Entities
			.AsNoTracking()
			.AsSplitQuery()
			.Include(c => c.ContentData)
			.FirstOrDefaultAsync(c => c.Id == id);

	public async Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId) =>
		await Entities
			.AsNoTracking()
			.AsSplitQuery()
			.Where(c => c.VolumeId == volumeId)
			.OrderBy(c => c.Order)
			.ToListAsync();

	public async Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId, int pageIndex, int pageSize) {
		var items = await Entities
			.AsNoTracking()
			.AsSplitQuery()
			.Where(c => c.VolumeId == volumeId)
			.OrderBy(c => c.Order)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

		return items;
	}

	public async Task<int> CountByVolumeId(Guid volumeId) =>
		await Entities
			.AsNoTracking()
			.Where(c => c.VolumeId == volumeId)
			.CountAsync();

	public async Task<IEnumerable<ChapterModel>> FindByVolumeIdsWithContent(IEnumerable<Guid> volumeIds) =>
		await Entities
			.AsNoTracking()
			.AsSplitQuery()
			.Include(c => c.ContentData)
			.Where(c => volumeIds.Contains(c.VolumeId))
			.OrderBy(c => c.VolumeId)
			.ThenBy(c => c.Order)
			.ToListAsync();
}
