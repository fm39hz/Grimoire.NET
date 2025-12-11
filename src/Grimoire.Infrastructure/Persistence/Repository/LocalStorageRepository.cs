namespace Grimoire.Infrastructure.Persistence.Repository;

using System.Security.Cryptography;
using Configuration;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public partial class LocalStorageRepository(
	ILogger<LocalStorageRepository> logger,
	IAssetRepository assetRepository,
	IOptions<StorageConfiguration> storageOptions)
	: IStorageRepository {
	private readonly StorageConfiguration _config = storageOptions.Value;

	private string StoragePath => _config.UseTemporaryDirectory
		? Path.Combine(Path.GetTempPath(), _config.BasePath)
		: _config.BasePath;

	public async Task<AssetModel> UploadAssetAsync(Guid seriesId, Stream content, string contentType,
		string originalFileName, string refType) {
		var hash = await ComputeHashAsync(content);

		var existingAsset = await assetRepository.GetBySeriesAndFileHashAsync(seriesId, hash);
		if (existingAsset is not null) {
			return existingAsset;
		}

		var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
		var assetPath = $"{_config.SeriesPath}/{seriesId}/{hash}{extension}";
		var filePath = Path.Combine(StoragePath, assetPath);

		var directory = Path.GetDirectoryName(filePath);
		if (directory is not null) {
			Directory.CreateDirectory(directory);
		}

		// Write file with exception handling for race conditions
		try {
			LogSavingFileToFilepath(logger, filePath);
			content.Seek(0, SeekOrigin.Begin);
			// Use FileMode.CreateNew to prevent overwriting if file was created by another thread
			await using var fileStream = new FileStream(filePath, FileMode.CreateNew);
			await content.CopyToAsync(fileStream);
		}
		catch (IOException ex) when (ex.Message.Contains("already exists") || File.Exists(filePath)) {
			// File was created by another concurrent operation, this is acceptable
			logger.LogDebug("File already exists at {FilePath}, continuing with asset creation", filePath);
		}

		var newAsset = new AssetModel {
			Id = Guid.CreateVersion7(),
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
			return [];
		}

		var filePath = Path.Combine(StoragePath, asset.Path);
		LogGettingFileFromFilepath(logger, filePath);
		return !File.Exists(filePath)? [] : await File.ReadAllBytesAsync(filePath);
	}

	public async Task<Stream?> GetFileStreamAsync(Guid assetId) {
		var asset = await assetRepository.FindOne(assetId);
		if (asset is null) {
			return null;
		}

		var filePath = Path.Combine(StoragePath, asset.Path);
		LogGettingFileFromFilepath(logger, filePath);

		if (!File.Exists(filePath)) {
			return null;
		}

		return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
	}

	public async Task DeleteFileAsync(Guid assetId) {
		var asset = await assetRepository.FindOne(assetId);
		if (asset is null) {
			return;
		}

		var filePath = Path.Combine(StoragePath, asset.Path);
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
