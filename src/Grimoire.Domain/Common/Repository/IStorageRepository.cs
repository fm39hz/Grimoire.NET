namespace Grimoire.Domain.Common.Repository;

using System.Threading;
using Entity.Book;

public interface IStorageRepository {
	public Task<AssetFileResult?> GetFileStreamAsync(Guid assetId, CancellationToken cancellationToken = default);
	public Task<AssetModel> UploadAssetAsync(Guid seriesId, Stream content, string contentType, string originalFileName,
		AssetRefType refType, string? prefix = null, CancellationToken cancellationToken = default);
	public Task<byte[]> GetFileAsync(Guid assetId, CancellationToken cancellationToken = default);
	public Task DeleteFileAsync(Guid assetId, CancellationToken cancellationToken = default);
}
