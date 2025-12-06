namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Exception;
using Dto.Book;
using Dto.Common;
using Mapper;

public sealed class VolumeService(
	IVolumeRepository repository,
	IChapterRepository chapterRepository,
	IBookMapper mapper) : IVolumeService {
	public async Task<VolumeModel?> FindOne(Guid id) => await repository.FindOne(id);

	public async Task<IEnumerable<VolumeModel>> FindAll() => await repository.FindAll();

	public async Task<PagedResult<VolumeModel>> FindAll(PaginationRequest request) {
		return await repository.FindAll(request.PageIndex, request.PageSize);
	}

	public async Task<VolumeModel> Create(CreateVolumeRequestDto dto) {
		var volume = mapper.CreateVolume(dto);
		return await repository.Create(volume);
	}

	public async Task<VolumeModel> Update(Guid id, UpdateVolumeRequestDto dto) {
		var volume = await repository.FindOne(id) ??
					throw new EntityNotFoundException($"Volume with id {id} not found");
		mapper.UpdateVolume(dto, volume);

		return await repository.Update(volume);
	}

	public async Task<int> Delete(Guid id) => await repository.Delete(id);

	public async Task<IEnumerable<ChapterModel>> FindAllChapters(Guid volumeId) =>
		await chapterRepository.FindByVolumeId(volumeId);

	public async Task<PagedResult<ChapterModel>> FindAllChapters(Guid volumeId, PaginationRequest pagination) {
		return await chapterRepository.FindByVolumeId(volumeId, pagination.PageIndex, pagination.PageSize);
	}
}
