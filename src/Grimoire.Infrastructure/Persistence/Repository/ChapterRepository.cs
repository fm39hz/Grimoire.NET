namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;

public sealed class ChapterRepository(ApplicationDbContext context)
	: CrudRepository<ChapterModel>(context), IChapterRepository {
}
