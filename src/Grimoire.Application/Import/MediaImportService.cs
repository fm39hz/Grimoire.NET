namespace Grimoire.Application.Import;

using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Common;
using Dto.Book;
using Service.Contract;
using Microsoft.Extensions.Logging;

public interface IMediaImportService {
    Task<Dictionary<string, string>> UploadFilesAsync(
        List<NormalizedFile> files, Guid seriesId,
        string prefix,
        Action<int>? onProgress = null,
        CancellationToken cancellationToken = default);

    Task UploadCoverAsync(
        CreateSeriesRequestDto seriesDto, Guid seriesId,
        NormalizedFile cover,
        CancellationToken cancellationToken = default);
}

public sealed class MediaImportService(
    IStorageService storageService,
    ISeriesService seriesService,
    ILogger<MediaImportService> logger) : IMediaImportService {

    public async Task<Dictionary<string, string>> UploadFilesAsync(
        List<NormalizedFile> files, Guid seriesId,
        string prefix,
        Action<int>? onProgress = null,
        CancellationToken cancellationToken = default) {

        var map = new Dictionary<string, string>();
        var processed = 0;

        foreach (var file in files)
        {
            using var stream = new MemoryStream(file.Content);
            var asset = await storageService.UploadAssetAsync(
                seriesId, stream,
                file.ContentType,
                file.FileName,
                AssetRefType.Content,
                prefix: prefix,
                cancellationToken: cancellationToken);

            map[file.FileName] = PrefixedId.ToString(EntityPrefix.Asset, asset.Id);

            processed++;
            onProgress?.Invoke(processed);
        }

        return map;
    }

    public async Task UploadCoverAsync(
        CreateSeriesRequestDto seriesDto, Guid seriesId,
        NormalizedFile cover,
        CancellationToken cancellationToken = default) {

        try
        {
            using var stream = new MemoryStream(cover.Content);
            var asset = await storageService.UploadAssetAsync(
                seriesId, stream,
                cover.ContentType,
                "cover",
                AssetRefType.Cover,
                cancellationToken: cancellationToken);

            seriesDto.Metadata.CoverImage = PrefixedId.ToString(EntityPrefix.Asset, asset.Id);
            await seriesService.Update(seriesId,
                new UpdateSeriesRequestDto(null, seriesDto.Metadata),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cover upload failed — Series={SeriesId}", seriesId);
        }
    }
}
