namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common;
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
	: CrudServiceBase<SeriesModel>, ISeriesService {
	public async Task<SeriesModel?> FindOne(Guid id) => await repository.FindOne(id);

	public async Task<PagedResult<SeriesModel>> FindAll(PaginationRequest request) =>
		await GetPagedResultAsync(repository, request);

	public async Task<SeriesModel> Create(CreateSeriesRequestDto dto) {
		var series = mapper.CreateSeries(dto);
		return await repository.Create(series);
	}

	public async Task<(SeriesModel Series, bool Created)> GetOrCreate(CreateSeriesRequestDto dto) {
		var existing = await repository.FindOneByTitle(dto.Title);
		if (existing is not null) {
			return (existing, false);
		}

		var series = mapper.CreateSeries(dto);
		var created = await repository.Create(series);
		return (created, true);
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

	public async Task<PagedResult<VolumeModel>> FindAllVolumes(Guid seriesId, PaginationRequest pagination) =>
		await GetPagedResultAsync(
			() => volumeRepository.FindBySeriesId(seriesId, pagination.PageIndex, pagination.PageSize),
			() => volumeRepository.CountBySeriesId(seriesId),
			pagination);
}
