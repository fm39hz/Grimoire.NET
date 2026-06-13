namespace Grimoire.Application.Service.Implementation;

using System.Threading;
using Contract;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Exception;
using Dto.Book;
using Strategy;

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
			_ = await seriesRepository.FindOne(seriesId, cancellationToken) ??
				throw new EntityNotFoundException($"Series with id {seriesId} not found");
 
			var existingVolumes = (await volumeNodeService.FindVolumes(seriesId, cancellationToken)).ToList();
			var volumesByOrder = existingVolumes.ToDictionary(v => v.Order);
 
			var volumeOrderToId = new Dictionary<double, Guid>();
 
			foreach (var volDto in request.Volumes) {
				Guid volId;
				if (volumesByOrder.TryGetValue(volDto.Order, out var existingVol)) {
					var updated = await volumeNodeService.UpdateVolume(
						existingVol.Id,
						new UpdateVolumeRequestDto(volDto.Order, volDto.Title, volDto.Metadata),
						cancellationToken);
					volId = updated.Id;
				} else {
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
			var chaptersByVolAndOrder = existingChapters.ToDictionary(c => (c.VolumeId, c.Order));
 
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
