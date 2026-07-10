namespace Grimoire.Application.Service.Implementation;

using System.Threading;
using Contract;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Exception;
using Dto.Book;

public sealed class SeriesSyncService(
	ISeriesRepository seriesRepository,
	IChapterRepository chapterRepository,
	IVolumeNodeService volumeNodeService,
	IChapterService chapterService,
	IAssetOwnershipService assetOwnershipService,
	IUnitOfWork unitOfWork) : ISeriesSyncService {

	public async Task SyncSeriesTree(Guid seriesId, SyncSeriesRequestDto request, CancellationToken cancellationToken = default) {
		await unitOfWork.BeginTransactionAsync(cancellationToken);
		try {
			var series = await seriesRepository.FindOne(seriesId, cancellationToken) ??
				throw new EntityNotFoundException($"Series with id {seriesId} not found");

			var volumes = await volumeNodeService.FindVolumes(seriesId, cancellationToken);
			var volumeOrderToId = volumes.ToDictionary(v => v.Order, v => v.Id);

			foreach (var volDto in request.Volumes) {
				if (!volumeOrderToId.TryGetValue(volDto.Order, out var volId)) {
					var created = await volumeNodeService.CreateVolume(new CreateVolumeRequestDto(
						PrefixedId.ToString(EntityPrefix.Series, seriesId),
						volDto.Order,
						volDto.Title,
						volDto.Metadata), cancellationToken);
					volId = created.Id;
				}
				volumeOrderToId[volDto.Order] = volId;
			}

			var volumeIds = volumeOrderToId.Values.ToList();
			var existingChapters = (await chapterRepository.FindByVolumeIdsWithContent(volumeIds, cancellationToken)).ToList();
			var chaptersByVolAndOrder = existingChapters.ToDictionary(c => (c.Path.GetVolumeId(), c.Order));

			foreach (var volDto in request.Volumes) {
				var volId = volumeOrderToId[volDto.Order];

				foreach (var chpDto in volDto.Chapters) {
					var key = (volId, chpDto.Order);
					var tempDto = new CreateChapterRequestDto(
						PrefixedId.ToString(EntityPrefix.Volume, volId),
						chpDto.Order,
						chpDto.Title,
						chpDto.Content,
						chpDto.Footnotes,
						chpDto.RawContent);

					chaptersByVolAndOrder.TryGetValue(key, out var existingChp);
					await chapterService.UpsertAsync(volId, tempDto, existingChp, cancellationToken);
				}
			}

			await assetOwnershipService.ReconcileSeriesAsync(seriesId, cancellationToken);
			await unitOfWork.CommitTransactionAsync(cancellationToken);
		}
		catch {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}
	}
}
