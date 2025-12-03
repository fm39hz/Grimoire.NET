namespace Grimoire.Application.Service.Contract;

using Domain.Entity.Book;
using Dto.Book;

public interface IChapterService : ICrudService<ChapterModel, CreateChapterRequestDto, UpdateChapterRequestDto> {
	public Task<ChapterModel> CreateFromImportAsync(CreateChapterRequestDto dto);
}
