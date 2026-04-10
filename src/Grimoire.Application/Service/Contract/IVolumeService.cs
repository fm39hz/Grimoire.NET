namespace Grimoire.Application.Service.Contract;

using Domain.Common;
using Domain.Entity.Book;
using Dto.Book;
using Dto.Common;

public interface IVolumeService : ICrudService<VolumeModel, CreateVolumeRequestDto, UpdateVolumeRequestDto> {
	public Task<IEnumerable<ChapterModel>> FindAllChapters(Guid volumeId);
	public Task<PagedResult<ChapterModel>> FindAllChapters(Guid volumeId, PaginationRequest pagination);
}
