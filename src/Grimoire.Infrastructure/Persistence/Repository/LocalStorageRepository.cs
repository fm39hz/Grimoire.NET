using System.Security.Cryptography;
using Grimoire.Domain.Common.Repository;
using Grimoire.Domain.Entity.Book;
using Grimoire.Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Grimoire.Infrastructure.Persistence.Repository;

public partial class LocalStorageRepository(
    ILogger<LocalStorageRepository> logger,
    ApplicationDbContext dbContext,
    IAssetRepository assetRepository) : IStorageRepository {
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), "grimoire-files");

    public async Task<AssetModel> UploadAssetAsync(Guid seriesId, Stream content, string contentType, string originalFileName, string refType) {
        var hash = await ComputeHashAsync(content);

        var existingAsset = await assetRepository.GetBySeriesAndFileHashAsync(seriesId, hash);
        if (existingAsset is not null) {
            return existingAsset;
        }

        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var assetPath = $"series/{seriesId}/{hash}{extension}";
        var filePath = Path.Combine(_storagePath, assetPath);

        var directory = Path.GetDirectoryName(filePath);
        if (directory is not null) {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(filePath)) {
            LogSavingFileToFilepath(logger, filePath);
            content.Seek(0, SeekOrigin.Begin);
            await using var fileStream = new FileStream(filePath, FileMode.Create);
            await content.CopyToAsync(fileStream);
        }

        var newAsset = new AssetModel {
            Id = Guid.NewGuid(),
            SeriesId = seriesId,
            Path = assetPath,
            FileHash = hash,
            RefType = refType
        };

        await assetRepository.Create(newAsset);
        return newAsset;
    }

    public async Task<byte[]> GetFileAsync(Guid assetId) {
        var asset = await assetRepository.FindOne(assetId);
        if (asset is null) {
            return Array.Empty<byte>();
        }

        var filePath = Path.Combine(_storagePath, asset.Path);
        LogGettingFileFromFilepath(logger, filePath);
        return !File.Exists(filePath) ? Array.Empty<byte>() : await File.ReadAllBytesAsync(filePath);
    }

    public async Task DeleteFileAsync(Guid assetId) {
        var asset = await assetRepository.FindOne(assetId);
        if (asset is null) {
            return;
        }

        var filePath = Path.Combine(_storagePath, asset.Path);
        LogDeletingFileFromFilepath(logger, filePath);
        if (File.Exists(filePath)) {
            File.Delete(filePath);
        }

        await assetRepository.Delete(assetId);
    }

    private static async Task<string> ComputeHashAsync(Stream stream) {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    [LoggerMessage(LogLevel.Information, "Saving file to {filePath}")]
    private static partial void LogSavingFileToFilepath(ILogger<LocalStorageRepository> logger, string filePath);

    [LoggerMessage(LogLevel.Information, "Getting file from {filePath}")]
    private static partial void LogGettingFileFromFilepath(ILogger<LocalStorageRepository> logger, string filePath);

    [LoggerMessage(LogLevel.Information, "Deleting file from {filePath}")]
    private static partial void LogDeletingFileFromFilepath(ILogger<LocalStorageRepository> logger, string filePath);
}
