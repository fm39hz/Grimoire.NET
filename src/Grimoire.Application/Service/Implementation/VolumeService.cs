namespace Grimoire.Application.Service.Implementation;

using System.Threading;
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
	ISeriesRepository seriesRepository,
	IChapterRepository chapterRepository,
	IBookMapper mapper) : CrudServiceBase<VolumeModel>, IVolumeService {
	public async Task<VolumeModel?> FindOne(Guid id, CancellationToken cancellationToken = default) => await repository.FindOne(id, cancellationToken);

	public async Task<PagedResult<VolumeModel>> FindAll(PaginationRequest request, CancellationToken cancellationToken = default) =>
		await GetPagedResultAsync(repository, request, cancellationToken);

	public async Task<VolumeModel> Create(CreateVolumeRequestDto dto, CancellationToken cancellationToken = default) {
		// Validate that the Series exists
		var seriesId = PrefixedId.ToGuid(dto.SeriesId, EntityPrefix.Series);
		var series = await seriesRepository.FindOne(seriesId, cancellationToken) ??
					throw new EntityNotFoundException($"Series with id {dto.SeriesId} not found");

		var volume = mapper.CreateVolume(dto, seriesId);
		return await repository.Create(volume, cancellationToken);
	}

	public async Task<VolumeModel> Update(Guid id, UpdateVolumeRequestDto dto, CancellationToken cancellationToken = default) {
		var volume = await repository.FindOne(id, cancellationToken) ??
					throw new EntityNotFoundException($"Volume with id {id} not found");
		mapper.UpdateVolume(dto, volume);

		return await repository.Update(volume, cancellationToken);
	}

	public async Task<int> Delete(Guid id, CancellationToken cancellationToken = default) => await repository.Delete(id, cancellationToken);

	public async Task<IEnumerable<ChapterModel>> FindAllChapters(Guid volumeId, CancellationToken cancellationToken = default) =>
		await chapterRepository.FindByVolumeId(volumeId, cancellationToken);

	public async Task<PagedResult<ChapterModel>> FindAllChapters(Guid volumeId,
		PaginationRequest pagination, CancellationToken cancellationToken = default) =>
		await GetPagedResultAsync(
			() => chapterRepository.FindByVolumeId(volumeId, pagination.PageIndex, pagination.PageSize, cancellationToken),
			() => chapterRepository.CountByVolumeId(volumeId, cancellationToken),
			pagination, cancellationToken);
}
