namespace Grimoire.Application.Service.Implementation;

using System.Threading;
using Contract;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Entity.Book.Metadata;
using Domain.Exception;
using Dto.Book;
using Mapper;
using Strategy;

public sealed class SeriesSyncService(
	ISeriesRepository seriesRepository,
	IVolumeRepository volumeRepository,
	IChapterRepository chapterRepository,
	ISourceMaterialRepository sourceRepository,
	IBookMapper mapper,
	IngestionStrategyFactory strategyFactory,
	IUnitOfWork unitOfWork) : ISeriesSyncService {

	public async Task SyncSeriesTree(Guid seriesId, SyncSeriesRequestDto request, CancellationToken cancellationToken = default) {
		await unitOfWork.BeginTransactionAsync(cancellationToken);
		try {
			_ = await seriesRepository.FindOne(seriesId, cancellationToken) ??
				throw new EntityNotFoundException($"Series with id {seriesId} not found");

			var existingVolumes = (await volumeRepository.FindBySeriesId(seriesId, cancellationToken)).ToList();
			var volumesByOrder = existingVolumes.ToDictionary(v => v.Order);

			var volumeOrderToId = new Dictionary<float, Guid>();

			foreach (var volDto in request.Volumes) {
				Guid volId;
				if (volumesByOrder.TryGetValue(volDto.Order, out var existingVol)) {
					existingVol.Title = volDto.Title;
					existingVol.Metadata = volDto.Metadata != null
						? new VolumeMetadata {
							CoverImage = volDto.Metadata.CoverImage,
							PublicationDate = volDto.Metadata.PublicationDate,
							Isbn = volDto.Metadata.Isbn
						}
						: null;
					await volumeRepository.Update(existingVol, cancellationToken);
					volId = existingVol.Id;
				} else {
					var createVolDto = new CreateVolumeRequestDto(
						PrefixedId.ToString(EntityPrefix.Series, seriesId),
						volDto.Order,
						volDto.Title,
						volDto.Metadata);
					var newVol = mapper.CreateVolume(createVolDto, seriesId);
					var created = await volumeRepository.Create(newVol, cancellationToken);
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
					} else {
						if (result.Source is not null) {
							await sourceRepository.Create(result.Source, cancellationToken);
						}

						result.Chapter.ContentData = result.Content;
						await chapterRepository.Create(result.Chapter, cancellationToken);
					}
				}
			}

			await unitOfWork.CommitTransactionAsync(cancellationToken);
		}
		catch {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}
	}
}
