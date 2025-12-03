namespace Grimoire.Application.Service.Contract;

using Domain.Entity.Book;
using Dto.Book;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IChapterService : ICrudService<ChapterModel, CreateChapterRequestDto, UpdateChapterRequestDto> {
	public Task<ChapterModel> CreateFromImportAsync(CreateChapterRequestDto dto);
    Task<IEnumerable<ChapterVariantResponseDto>> GetVariantsByIdsAsync(IEnumerable<Guid> ids);
    Task<IEnumerable<ChapterVariantResponseDto>> GetVariantsByChapterIdAsync(Guid chapterId, VariantType[]? types);
}
