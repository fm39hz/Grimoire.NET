namespace Grimoire.Application.Service.Contract;

using Domain.Entity.Book;
using Dto.Book;

public interface IChapterService : ICrudService<ChapterModel> {
	public Task<ChapterModel> CreateFromImportAsync(ChapterRequestDto dto);
}
