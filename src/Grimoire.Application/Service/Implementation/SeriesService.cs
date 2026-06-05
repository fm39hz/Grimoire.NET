namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Exception;
using Dto.Book;
using Dto.Common;
using Mapper;
using System.Threading;

public sealed class SeriesService(
	ISeriesRepository repository,
	IVolumeRepository volumeRepository,
	IBookMapper mapper)
	: CrudServiceBase<SeriesModel>, ISeriesService {
	public async Task<SeriesModel?> FindOne(Guid id, CancellationToken cancellationToken = default) => await repository.FindOne(id, cancellationToken);

	public async Task<PagedResult<SeriesModel>> FindAll(PaginationRequest request, CancellationToken cancellationToken = default) =>
		await GetPagedResultAsync(repository, request, cancellationToken);

	public async Task<SeriesModel> Create(CreateSeriesRequestDto dto, CancellationToken cancellationToken = default) {
		var series = mapper.CreateSeries(dto);
		return await repository.Create(series, cancellationToken);
	}

	public async Task<(SeriesModel Series, bool Created)> GetOrCreate(CreateSeriesRequestDto dto, CancellationToken cancellationToken = default) {
		var existing = await repository.FindOneByTitle(dto.Title, cancellationToken);
		if (existing is not null) {
			return (existing, false);
		}

		var series = mapper.CreateSeries(dto);
		var created = await repository.Create(series, cancellationToken);
		return (created, true);
	}

	public async Task<SeriesModel> Update(Guid id, UpdateSeriesRequestDto dto, CancellationToken cancellationToken = default) {
		var series = await repository.FindOne(id, cancellationToken) ??
					throw new EntityNotFoundException($"Series with id {id} not found");
		mapper.UpdateSeries(dto, series);
		return await repository.Update(series, cancellationToken);
	}

	public async Task<int> Delete(Guid id, CancellationToken cancellationToken = default) => await repository.Delete(id, cancellationToken);

	public async Task<IEnumerable<VolumeModel>> FindAllVolumes(Guid seriesId, CancellationToken cancellationToken = default) =>
		await volumeRepository.FindBySeriesId(seriesId, cancellationToken);

	public async Task<PagedResult<VolumeModel>> FindAllVolumes(Guid seriesId, PaginationRequest pagination, CancellationToken cancellationToken = default) =>
		await GetPagedResultAsync(
			() => volumeRepository.FindBySeriesId(seriesId, pagination.PageIndex, pagination.PageSize, cancellationToken),
			() => volumeRepository.CountBySeriesId(seriesId, cancellationToken),
			pagination, cancellationToken);
}
