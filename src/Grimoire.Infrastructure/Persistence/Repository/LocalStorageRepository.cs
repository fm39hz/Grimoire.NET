namespace Grimoire.Infrastructure.Persistence.Repository;

using System.Security.Cryptography;
using System.Threading;
using Configuration;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public partial class LocalStorageRepository(
	IAssetRepository assetRepository,
	IOptions<StorageConfiguration> storageOptions)
	: IStorageRepository {
	private readonly StorageConfiguration _config = storageOptions.Value;

	private string StoragePath => _config.UseTemporaryDirectory
		? Path.GetTempPath()
		: _config.BasePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage");

	// ── Asset-bound API ──────────────────────────────────────────────

	public async Task<AssetModel> UploadAssetAsync(Guid seriesId, Stream content,
		string contentType, string originalFileName, AssetRefType refType, string? prefix = null,
		CancellationToken cancellationToken = default) {
		var hash = await ComputeHashAsync(content, cancellationToken);

		var existing = await assetRepository.GetByFileHashAsync(hash, cancellationToken);
		if (existing is not null) return existing;

		var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
		var assetPath = prefix is not null
			? Path.Combine(prefix, $"{hash}{extension}")
			: Path.Combine("series", seriesId.ToString(), $"{hash}{extension}");

		var filePath = Path.Combine(StoragePath, assetPath);
		Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

		content.Seek(0, SeekOrigin.Begin);
		await using var fileStream = File.Create(filePath);
		await content.CopyToAsync(fileStream, cancellationToken);

		var asset = new AssetModel {
			Id = Guid.CreateVersion7(),
			SeriesId = seriesId,
			OwnerNodeId = seriesId,
			Path = assetPath,
			FileHash = hash,
			RefType = refType,
			ContentType = contentType,
			OriginalFileName = Path.GetFileName(originalFileName)
		};

		await assetRepository.Create(asset, cancellationToken);
		return asset;
	}

	public async Task<AssetFileResult?> GetFileStreamAsync(Guid assetId, CancellationToken cancellationToken = default) {
		var asset = await assetRepository.FindOne(assetId, cancellationToken);
		if (asset is null) return null;

		var filePath = Path.Combine(StoragePath, asset.Path);
		if (!File.Exists(filePath)) return null;

		return new AssetFileResult {
			Stream = File.OpenRead(filePath),
			ContentType = asset.ContentType,
			FileName = asset.OriginalFileName
		};
	}

	public async Task<byte[]> GetFileAsync(Guid assetId, CancellationToken cancellationToken = default) {
		var asset = await assetRepository.FindOne(assetId, cancellationToken);
		if (asset is null) return [];

		var filePath = Path.Combine(StoragePath, asset.Path);
		return !File.Exists(filePath) ? [] : await File.ReadAllBytesAsync(filePath, cancellationToken);
	}

	public async Task DeleteFileAsync(Guid assetId, CancellationToken cancellationToken = default) {
		var asset = await assetRepository.FindOne(assetId, cancellationToken);
		if (asset is null) return;

		var filePath = Path.Combine(StoragePath, asset.Path);
		if (File.Exists(filePath)) File.Delete(filePath);
		await assetRepository.Delete(assetId, cancellationToken);
	}

	// ── Raw file API (no AssetModel) ──────────────────────────────────

	public async Task<string> UploadFileAsync(Stream content, string contentType, string fileName,
		string? prefix = null, CancellationToken cancellationToken = default) {
		var hash = await ComputeHashAsync(content, cancellationToken);
		var ext = Path.GetExtension(fileName).ToLowerInvariant();
		var objectKey = prefix is not null
			? Path.Combine(prefix, $"{hash}{ext}")
			: Path.Combine("uploads", $"{hash}{ext}");

		var filePath = Path.Combine(StoragePath, objectKey);
		Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

		content.Seek(0, SeekOrigin.Begin);
		await using var fileStream = File.Create(filePath);
		await content.CopyToAsync(fileStream, cancellationToken);

		return objectKey;
	}

	public Task<Stream?> GetFileByPathAsync(string filePath, CancellationToken cancellationToken = default) {
		var fullPath = Path.Combine(StoragePath, filePath);
		if (!File.Exists(fullPath)) return Task.FromResult<Stream?>(null);
		return Task.FromResult<Stream?>(File.OpenRead(fullPath));
	}

	public Task DeleteFileByPathAsync(string filePath, CancellationToken cancellationToken = default) {
		var fullPath = Path.Combine(StoragePath, filePath);
		if (File.Exists(fullPath)) File.Delete(fullPath);
		return Task.CompletedTask;
	}

	// ── helpers ──────────────────────────────────────────────────────

	private static async Task<string> ComputeHashAsync(Stream stream, CancellationToken cancellationToken = default) {
		using var sha256 = SHA256.Create();
		var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
		return Convert.ToHexString(hashBytes).ToLowerInvariant();
	}
}
