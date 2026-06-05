namespace Grimoire.Application.Service.Contract;

using System.Threading;
using Domain.Entity.Book;
using Dto.Book;

public interface IChapterService : ICrudService<ChapterModel, CreateChapterRequestDto, UpdateChapterRequestDto> {
	public Task<ChapterModel> CreateFromImportAsync(CreateChapterRequestDto dto, CancellationToken cancellationToken = default);
	public Task<IEnumerable<ChapterModel>> SplitAsync(Guid chapterId, SplitChapterRequestDto dto, CancellationToken cancellationToken = default);
}
