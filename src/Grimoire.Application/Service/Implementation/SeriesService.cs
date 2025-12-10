namespace Grimoire.Application.Service.Implementation;

using Common;
using Contract;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Exception;
using Dto.Book;
using Dto.Common;
using Mapper;

public sealed class SeriesService(
	ISeriesRepository repository,
	IVolumeRepository volumeRepository,
	IBookMapper mapper)
	: ISeriesService {
	public async Task<SeriesModel?> FindOne(Guid id) => await repository.FindOne(id);

	public async Task<IEnumerable<SeriesModel>> FindAll() => await repository.FindAll();

	public async Task<PagedResult<SeriesModel>> FindAll(PaginationRequest request) {
		var items = await repository.FindAll(request.PageIndex, request.PageSize);
		var totalCount = await repository.CountAll();
		return new PagedResult<SeriesModel>(items.ToList(), totalCount, request.PageIndex, request.PageSize);
	}

	public async Task<SeriesModel> Create(CreateSeriesRequestDto dto) {
		var series = mapper.CreateSeries(dto);
		return await repository.Create(series);
	}

	public async Task<SeriesModel> Update(Guid id, UpdateSeriesRequestDto dto) {
		var series = await repository.FindOne(id) ??
					throw new EntityNotFoundException($"Series with id {id} not found");
		mapper.UpdateSeries(dto, series);
		return await repository.Update(series);
	}

	public async Task<int> Delete(Guid id) => await repository.Delete(id);

	public async Task<IEnumerable<VolumeModel>> FindAllVolumes(Guid seriesId) =>
		await volumeRepository.FindBySeriesId(seriesId);

	public async Task<PagedResult<VolumeModel>> FindAllVolumes(Guid seriesId, PaginationRequest pagination) {
		var items = await volumeRepository.FindBySeriesId(seriesId, pagination.PageIndex, pagination.PageSize);
		var totalCount = await volumeRepository.CountBySeriesId(seriesId);
		return new PagedResult<VolumeModel>(items.ToList(), totalCount, pagination.PageIndex, pagination.PageSize);
	}
}
