namespace Grimoire.Domain.Common.Repository;

using Entity.Book;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IChapterRepository : IRepository<ChapterModel> {
    Task<IEnumerable<ChapterVariantModel>> FindVariantsByIdsAsync(IEnumerable<Guid> ids);
    Task<IEnumerable<ChapterVariantModel>> FindVariantsByChapterIdAsync(Guid chapterId);
}
