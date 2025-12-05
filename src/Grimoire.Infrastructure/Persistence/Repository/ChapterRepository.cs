namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;

public sealed class ChapterRepository(ApplicationDbContext context)
	: CrudRepository<ChapterModel>(context), IChapterRepository {
	public async Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId) =>
		await Entities
			.Where(c => c.VolumeId == volumeId)
			.OrderBy(c => c.Order)
			.ToListAsync();
}
