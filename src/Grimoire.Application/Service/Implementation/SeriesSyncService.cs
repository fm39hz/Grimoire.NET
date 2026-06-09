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
	ISourceMaterialRepository sourceRepository,
	IBookTreeService bookTreeService,
	IAssetOwnershipService assetOwnershipService,
	IngestionStrategyFactory strategyFactory,
	IUnitOfWork unitOfWork) : ISeriesSyncService {

	public async Task SyncSeriesTree(Guid seriesId, SyncSeriesRequestDto request, CancellationToken cancellationToken = default) {
		await unitOfWork.BeginTransactionAsync(cancellationToken);
		try {
			_ = await seriesRepository.FindOne(seriesId, cancellationToken) ??
				throw new EntityNotFoundException($"Series with id {seriesId} not found");

			var existingVolumes = (await bookTreeService.FindVolumes(seriesId, cancellationToken)).ToList();
			var volumesByOrder = existingVolumes.ToDictionary(v => v.Order);

			var volumeOrderToId = new Dictionary<float, Guid>();

			foreach (var volDto in request.Volumes) {
				Guid volId;
				if (volumesByOrder.TryGetValue(volDto.Order, out var existingVol)) {
					var updated = await bookTreeService.UpdateVolume(
						existingVol.Id,
						new UpdateVolumeRequestDto(volDto.Order, volDto.Title, volDto.Metadata),
						cancellationToken);
					volId = updated.Id;
				} else {
					var created = await bookTreeService.CreateVolume(new CreateVolumeRequestDto(
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
					var key = (volId, (float)chpDto.Order);
					var tempDto = new CreateChapterRequestDto(
						PrefixedId.ToString(EntityPrefix.Volume, volId),
						chpDto.Order,
						chpDto.Title,
						chpDto.Content,
						chpDto.Footnotes,
						chpDto.RawContent);

					var strategy = strategyFactory.GetStrategy(tempDto);
					var result = await strategy.ExecuteAsync(tempDto, volId, cancellationToken);

					if (chaptersByVolAndOrder.TryGetValue(key, out var existingChp)) {
						existingChp.Title = result.Chapter.Title;
						existingChp.Status = result.Chapter.Status;

						if (existingChp.ContentData != null) {
							existingChp.ContentData.Segments = result.Content.Segments;
							existingChp.ContentData.Footnotes = result.Content.Footnotes;
						} else {
							existingChp.ContentData = new ChapterContentModel {
								Id = existingChp.Id,
								Segments = result.Content.Segments,
								Footnotes = result.Content.Footnotes
							};
						}

						if (result.Source is not null) {
							await sourceRepository.Create(result.Source, cancellationToken);
						}

						await chapterRepository.Update(existingChp, cancellationToken);
						await bookTreeService.UpdateNode(existingChp.Id, existingChp.Title, existingChp.Order, cancellationToken);
					} else {
						if (result.Source is not null) {
							await sourceRepository.Create(result.Source, cancellationToken);
						}

						result.Chapter.ContentData = result.Content;
						var chapter = await chapterRepository.Create(result.Chapter, cancellationToken);
						await bookTreeService.CreateNode(
							chapter.Id,
							BookNodeType.Chapter,
							volId,
							chapter.Title,
							chapter.Order,
							cancellationToken);
					}
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
