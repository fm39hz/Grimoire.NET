namespace Grimoire.Application.Service.Implementation;

using Common;
using Contract;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Exception;
using Dto.Book;
using Dto.Common;
using Mapper;
using DomainCommon = Domain.Common;

public sealed class VolumeService(
	IVolumeRepository repository,
	ISeriesRepository seriesRepository,
	IChapterRepository chapterRepository,
	IBookMapper mapper) : CrudServiceBase<VolumeModel>, IVolumeService {
	public async Task<VolumeModel?> FindOne(Guid id) => await repository.FindOne(id);

	public async Task<IEnumerable<VolumeModel>> FindAll() => await repository.FindAll();

	public async Task<PagedResult<VolumeModel>> FindAll(PaginationRequest request) =>
		await GetPagedResultAsync(repository, request);

	public async Task<VolumeModel> Create(CreateVolumeRequestDto dto) {
		// Validate that the Series exists
		var seriesId = DomainCommon.PrefixedId.ToGuid(dto.SeriesId, DomainCommon.EntityPrefix.Series);
		var series = await seriesRepository.FindOne(seriesId);
		if (series == null) {
			throw new EntityNotFoundException($"Series with id {dto.SeriesId} not found");
		}

		var volume = mapper.CreateVolume(dto, seriesId);
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

	public async Task<PagedResult<ChapterModel>> FindAllChapters(Guid volumeId, PaginationRequest pagination) =>
		await GetPagedResultAsync(
			() => chapterRepository.FindByVolumeId(volumeId, pagination.PageIndex, pagination.PageSize),
			() => chapterRepository.CountByVolumeId(volumeId),
			pagination);
}
