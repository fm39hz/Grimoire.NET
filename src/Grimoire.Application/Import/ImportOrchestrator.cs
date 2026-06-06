namespace Grimoire.Application.Import;

using Domain.Common;
using Domain.Common.Repository;
using Dto.Book;
using Service.Contract;
using Microsoft.Extensions.Logging;

public sealed class ImportOrchestrator(
    ISeriesService seriesService,
    IVolumeTreeResolver volumeResolver,
    IChapterImportHandler chapterHandler,
    IMediaImportService mediaService,
    IUnitOfWork unitOfWork,
    ILogger<ImportOrchestrator> logger) : IImportOrchestrator {

    public async Task<ImportEpubResultDto> ImportAsync(
        IImportStrategy strategy,
        CreateSeriesRequestDto seriesDto,
        List<ImportVolumeDto>? volumesOverride,
        Stream sourceStream,
        CancellationToken cancellationToken = default) {

        var normalized = await strategy.ParseAsync(sourceStream, cancellationToken);
        var merged = BuildVolumeList(normalized, volumesOverride);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var (series, _) = await seriesService.GetOrCreate(seriesDto, cancellationToken);
            var seriesId = series.Id;

            var prefix = $"staging/import/{seriesId}";
            var fileMap = await mediaService.UploadFilesAsync(normalized.Files, seriesId, prefix, cancellationToken);

            if (normalized.Cover is not null)
                await mediaService.UploadCoverAsync(seriesDto, seriesId, normalized.Cover, cancellationToken);

            var vols = await volumeResolver.ResolveAsync(seriesId, merged, cancellationToken);

            var chaptersCreated = 0;
            var chaptersUpdated = 0;

            foreach (var vol in vols)
            {
                var volEntry = merged.First(v => v.Order == vol.VolumeOrder);

                foreach (var ch in vol.Chapters)
                {
                    var chEntry = volEntry.Chapters.FirstOrDefault(c => c.Order == ch.Order);
                    if (chEntry is null) continue;

                    var result = await chapterHandler.ImportAsync(
                        vol.Id, chEntry, fileMap, cancellationToken);

                    if (result.Created) chaptersCreated++;
                    else chaptersUpdated++;
                }
            }

            await unitOfWork.CommitTransactionAsync(cancellationToken);

            logger.LogInformation(
                "Import completed — Series={SeriesId}, Volumes: {Created}/{Updated}, Chapters: {Created}/{Updated}",
                seriesId,
                vols.Count(v => v.WasCreated), vols.Count(v => !v.WasCreated),
                chaptersCreated, chaptersUpdated);

            return new ImportEpubResultDto(
                SeriesId: seriesId,
                VolumesCreated: vols.Count(v => v.WasCreated),
                VolumesUpdated: vols.Count(v => !v.WasCreated),
                ChaptersCreated: chaptersCreated,
                ChaptersUpdated: chaptersUpdated);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static List<NormalizedVolume> BuildVolumeList(
        NormalizedImport normalized,
        List<ImportVolumeDto>? overrideVols) {

        if (overrideVols?.Count > 0)
            return overrideVols.Select(v => new NormalizedVolume
            {
                Order = v.Order,
                Title = v.Title ?? normalized.Title,
                Chapters = v.Chapters?.Select(c =>
                {
                    var match = normalized.Volumes
                        .SelectMany(x => x.Chapters)
                        .FirstOrDefault(x => x.Order == c.Order);
                    return new NormalizedChapter
                    {
                        Order = c.Order,
                        Title = c.Title ?? match?.Title ?? $"Chapter {c.Order}",
                        Segments = match?.Segments ?? [],
                        Footnotes = match?.Footnotes ?? [],
                        RawHtml = match?.RawHtml
                    };
                }).ToList() ?? []
            }).ToList();

        return normalized.Volumes;
    }
}
