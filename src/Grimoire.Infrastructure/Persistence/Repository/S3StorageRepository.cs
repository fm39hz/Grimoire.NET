namespace Grimoire.Infrastructure.Persistence.Repository;

using System.Security.Cryptography;
using System.Threading;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Configuration;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed partial class S3StorageRepository(
	ILogger<S3StorageRepository> logger,
	IAssetRepository assetRepository,
	IOptions<S3Configuration> s3Options
) : IStorageRepository {
	private readonly AmazonS3Client _s3Client = CreateClient(s3Options.Value);
	private readonly S3Configuration _config = s3Options.Value;

	private static AmazonS3Client CreateClient(S3Configuration cfg) {
		var awsConfig = new AmazonS3Config {
			ServiceURL = cfg.Endpoint,
			ForcePathStyle = true,
			AuthenticationRegion = cfg.Region,
			UseHttp = !cfg.UseSsl
		};
		return new AmazonS3Client(cfg.AccessKey, cfg.SecretKey, awsConfig);
	}

	// ── Asset-bound API (with AssetModel record) ──────────────────────

	public async Task<AssetModel> UploadAssetAsync(Guid seriesId, Stream content,
		string contentType, string originalFileName, AssetRefType refType, string? prefix = null,
		CancellationToken cancellationToken = default) {
		await EnsureBucketAsync(cancellationToken);

		var hash = await ComputeHashAsync(content, cancellationToken);

		if (prefix is null) {
			var existing = await assetRepository.GetBySeriesAndFileHashAsync(seriesId, hash, cancellationToken);
			if (existing is not null) {
				return existing;
			}
		}

		var objectKey = BuildKey(originalFileName, hash, prefix, seriesId);

		LogUploadingToS3(logger, _config.BucketName, objectKey);
		content.Seek(0, SeekOrigin.Begin);

		using var transferUtility = new TransferUtility(_s3Client);
		await transferUtility.UploadAsync(new TransferUtilityUploadRequest {
			BucketName = _config.BucketName,
			Key = objectKey,
			InputStream = content,
			ContentType = contentType
		}, cancellationToken);

		var asset = new AssetModel {
			Id = Guid.CreateVersion7(),
			SeriesId = seriesId,
			Path = objectKey,
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

		return await GetStreamByKeyAsync(asset.Path, asset.ContentType, asset.OriginalFileName, cancellationToken);
	}

	public async Task<byte[]> GetFileAsync(Guid assetId, CancellationToken cancellationToken = default) {
		var asset = await assetRepository.FindOne(assetId, cancellationToken);
		if (asset is null) return [];

		return await GetBytesByKeyAsync(asset.Path, cancellationToken);
	}

	public async Task DeleteFileAsync(Guid assetId, CancellationToken cancellationToken = default) {
		var asset = await assetRepository.FindOne(assetId, cancellationToken);
		if (asset is null) return;

		await DeleteByKeyAsync(asset.Path, cancellationToken);
		await assetRepository.Delete(assetId, cancellationToken);
	}

	// ── Raw file API (no AssetModel) ──────────────────────────────────

	public async Task<string> UploadFileAsync(Stream content, string contentType, string fileName,
		string? prefix = null, CancellationToken cancellationToken = default) {
		await EnsureBucketAsync(cancellationToken);

		var hash = await ComputeHashAsync(content, cancellationToken);
		var ext = Path.GetExtension(fileName).ToLowerInvariant();
		var objectKey = prefix is not null
			? $"{prefix.TrimEnd('/')}/{hash}{ext}"
			: $"uploads/{hash}{ext}";

		LogUploadingToS3(logger, _config.BucketName, objectKey);
		content.Seek(0, SeekOrigin.Begin);

		using var transferUtility = new TransferUtility(_s3Client);
		await transferUtility.UploadAsync(new TransferUtilityUploadRequest {
			BucketName = _config.BucketName,
			Key = objectKey,
			InputStream = content,
			ContentType = contentType
		}, cancellationToken);

		return objectKey;
	}

	public async Task<Stream?> GetFileByPathAsync(string filePath, CancellationToken cancellationToken = default) {
		try {
			var response = await _s3Client.GetObjectAsync(new GetObjectRequest {
				BucketName = _config.BucketName,
				Key = filePath
			}, cancellationToken);

			return response.ResponseStream;
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) {
			return null;
		}
	}

	public async Task DeleteFileByPathAsync(string filePath, CancellationToken cancellationToken = default) {
		await _s3Client.DeleteObjectAsync(new DeleteObjectRequest {
			BucketName = _config.BucketName,
			Key = filePath
		}, cancellationToken);
	}

	// ── helpers ──────────────────────────────────────────────────────

	private bool _bucketInitialized;
	private readonly Lock _bucketLock = new();

	private async Task EnsureBucketAsync(CancellationToken cancellationToken = default) {
		if (_bucketInitialized) return;

		lock (_bucketLock) {
			if (_bucketInitialized) return;
		}

		var buckets = await _s3Client.ListBucketsAsync(cancellationToken);
		var bucketExists = buckets.Buckets.Exists(b => b.BucketName == _config.BucketName);

		if (!bucketExists) {
			LogCreatingBucket(logger, _config.BucketName);
			await _s3Client.PutBucketAsync(new PutBucketRequest {
				BucketName = _config.BucketName,
				UseClientRegion = true
			}, cancellationToken);
		}

		_bucketInitialized = true;
	}

	private static async Task<string> ComputeHashAsync(Stream stream, CancellationToken cancellationToken = default) {
		using var sha256 = SHA256.Create();
		var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
		return Convert.ToHexString(hashBytes).ToLowerInvariant();
	}

	private static string BuildKey(string fileName, string hash, string? prefix, Guid seriesId) {
		var ext = Path.GetExtension(fileName).ToLowerInvariant();
		return prefix is not null
			? $"{prefix.TrimEnd('/')}/{hash}{ext}"
			: $"series/{seriesId}/{hash}{ext}";
	}

	private async Task<AssetFileResult?> GetStreamByKeyAsync(string key, string contentType, string fileName, CancellationToken ct) {
		LogGettingFromS3(logger, _config.BucketName, key);

		try {
			var response = await _s3Client.GetObjectAsync(new GetObjectRequest {
				BucketName = _config.BucketName,
				Key = key
			}, ct);

			return new AssetFileResult {
				Stream = response.ResponseStream,
				ContentType = response.Headers.ContentType ?? contentType,
				FileName = fileName
			};
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) {
			return null;
		}
	}

	private async Task<byte[]> GetBytesByKeyAsync(string key, CancellationToken ct) {
		try {
			var response = await _s3Client.GetObjectAsync(new GetObjectRequest {
				BucketName = _config.BucketName,
				Key = key
			}, ct);

			await using var stream = response.ResponseStream;
			using var memoryStream = new MemoryStream();
			await stream.CopyToAsync(memoryStream, ct);
			return memoryStream.ToArray();
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) {
			return [];
		}
	}

	private async Task DeleteByKeyAsync(string key, CancellationToken ct) {
		LogDeletingFromS3(logger, _config.BucketName, key);
		await _s3Client.DeleteObjectAsync(new DeleteObjectRequest {
			BucketName = _config.BucketName,
			Key = key
		}, ct);
	}

	// ── logging ──────────────────────────────────────────────────────

	[LoggerMessage(LogLevel.Information, "Uploading to S3: bucket={Bucket} key={Key}")]
	private static partial void LogUploadingToS3(ILogger logger, string bucket, string key);

	[LoggerMessage(LogLevel.Information, "Getting from S3: bucket={Bucket} key={Key}")]
	private static partial void LogGettingFromS3(ILogger logger, string bucket, string key);

	[LoggerMessage(LogLevel.Information, "Deleting from S3: bucket={Bucket} key={Key}")]
	private static partial void LogDeletingFromS3(ILogger logger, string bucket, string key);

	[LoggerMessage(LogLevel.Information, "Creating S3 bucket: {Bucket}")]
	private static partial void LogCreatingBucket(ILogger logger, string bucket);
}
