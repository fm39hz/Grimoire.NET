namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Dto.Book;
using Dto.Common;
using System.Threading;

public sealed class SeriesService(
	ISeriesRepository repository,
	ISeriesNodeService seriesNodeService,
	IVolumeNodeService volumeNodeService,
	INodeManagerService nodeManagerService)
	: CrudServiceBase<SeriesModel>, ISeriesService {
	public async Task<SeriesModel?> FindOne(Guid id, CancellationToken cancellationToken = default) =>
		await seriesNodeService.FindSeries(id, cancellationToken);

	public async Task<PagedResult<SeriesModel>> FindAll(PaginationRequest request, CancellationToken cancellationToken = default) =>
		await GetPagedResultAsync(repository, request, cancellationToken);

	public async Task<SeriesModel> Create(CreateSeriesRequestDto dto, CancellationToken cancellationToken = default) {
		return await seriesNodeService.CreateSeries(dto, cancellationToken);
	}

	public async Task<(SeriesModel Series, bool Created)> GetOrCreate(CreateSeriesRequestDto dto, CancellationToken cancellationToken = default) =>
		await seriesNodeService.GetOrCreateSeries(dto, cancellationToken);

	public async Task<SeriesModel> Update(Guid id, UpdateSeriesRequestDto dto, CancellationToken cancellationToken = default) {
		return await seriesNodeService.UpdateSeries(id, dto, cancellationToken);
	}

	public async Task<int> Delete(Guid id, CancellationToken cancellationToken = default) => await nodeManagerService.DeleteSubtree(id, cancellationToken);

	public async Task<IEnumerable<VolumeModel>> FindAllVolumes(Guid seriesId, CancellationToken cancellationToken = default) =>
		await volumeNodeService.FindVolumes(seriesId, cancellationToken);

	public async Task<PagedResult<VolumeModel>> FindAllVolumes(Guid seriesId, PaginationRequest pagination, CancellationToken cancellationToken = default) =>
		await volumeNodeService.FindVolumes(seriesId, pagination, cancellationToken);
}
