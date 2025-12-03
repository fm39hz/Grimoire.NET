namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class ChapterRepository(ApplicationDbContext context)
	: CrudRepository<ChapterModel>(context), IChapterRepository {
    public async Task<IEnumerable<ChapterVariantModel>> FindVariantsByIdsAsync(IEnumerable<Guid> ids) {
        return await context.ChapterVariants
            .Where(v => ids.Contains(v.Id))
            .ToListAsync();
    }

    public async Task<IEnumerable<ChapterVariantModel>> FindVariantsByChapterIdAsync(Guid chapterId) {
        return await context.ChapterVariants
            .Where(v => v.ChapterId == chapterId)
            .ToListAsync();
    }
}
