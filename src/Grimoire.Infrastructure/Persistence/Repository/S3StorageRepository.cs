namespace Grimoire.Infrastructure.Persistence.Repository;

using System.Security.Cryptography;
using System.Threading;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Configuration;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
///     S3-compatible storage repository using AWSSDK.S3.
///     Works with any S3-compatible service (custom, Minio, R2, Backblaze, etc.)
///     via ServiceURL + ForcePathStyle configuration.
///     Thread-safe; AmazonS3Client is singleton-safe.
///     Bucket is auto-created on first use.
/// </summary>
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

	// ── public API ───────────────────────────────────────────────────

	/// <summary>
	///     Deduplicates by SHA256 hash within a series, then uploads to S3.
	///     Object key: series/{seriesId}/{hash}{ext}
	/// </summary>
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

		var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
		var objectKey = prefix is not null
			? $"{prefix}/{hash}{extension}"
			: $"series/{seriesId}/{hash}{extension}";

		LogUploadingToS3(logger, _config.BucketName, objectKey);
		content.Seek(0, SeekOrigin.Begin);

		var putRequest = new PutObjectRequest {
			BucketName = _config.BucketName,
			Key = objectKey,
			InputStream = content,
			ContentType = contentType,
			AutoResetStreamPosition = false
		};

		await _s3Client.PutObjectAsync(putRequest, cancellationToken);

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

	/// <summary>
	///     Returns direct stream from S3. Caller must dispose.
	/// </summary>
	public async Task<AssetFileResult?> GetFileStreamAsync(Guid assetId, CancellationToken cancellationToken = default) {
		var asset = await assetRepository.FindOne(assetId, cancellationToken);
		if (asset is null) {
			return null;
		}

		LogGettingFromS3(logger, _config.BucketName, asset.Path);

		try {
			var response = await _s3Client.GetObjectAsync(new GetObjectRequest {
				BucketName = _config.BucketName,
				Key = asset.Path
			}, cancellationToken);

			return new AssetFileResult {
				Stream = response.ResponseStream,
				ContentType = response.Headers.ContentType ?? asset.ContentType,
				FileName = asset.OriginalFileName
			};
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) {
			return null;
		}
	}

	/// <summary>
	///     Downloads object as byte array.
	/// </summary>
	public async Task<byte[]> GetFileAsync(Guid assetId, CancellationToken cancellationToken = default) {
		var asset = await assetRepository.FindOne(assetId, cancellationToken);
		if (asset is null) {
			return [];
		}

		try {
			var response = await _s3Client.GetObjectAsync(new GetObjectRequest {
				BucketName = _config.BucketName,
				Key = asset.Path
			}, cancellationToken);

			await using var stream = response.ResponseStream;
			using var memoryStream = new MemoryStream();
			await stream.CopyToAsync(memoryStream, cancellationToken);
			return memoryStream.ToArray();
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) {
			return [];
		}
	}

	/// <summary>
	///     Deletes the object from S3 and the asset record from DB.
	/// </summary>
	public async Task DeleteFileAsync(Guid assetId, CancellationToken cancellationToken = default) {
		var asset = await assetRepository.FindOne(assetId, cancellationToken);
		if (asset is null) {
			return;
		}

		LogDeletingFromS3(logger, _config.BucketName, asset.Path);

		await _s3Client.DeleteObjectAsync(new DeleteObjectRequest {
			BucketName = _config.BucketName,
			Key = asset.Path
		}, cancellationToken);

		await assetRepository.Delete(assetId, cancellationToken);
	}

	// ── helpers ──────────────────────────────────────────────────────

	private bool _bucketInitialized;
	private readonly Lock _bucketLock = new();

	private async Task EnsureBucketAsync(CancellationToken cancellationToken = default) {
		if (_bucketInitialized) {
			return;
		}

		lock (_bucketLock) {
			if (_bucketInitialized) {
				return;
			}
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
