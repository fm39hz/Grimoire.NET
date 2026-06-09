namespace Grimoire.Application.Service.Contract;

using System.Threading;
using Domain.Common;
using Domain.Entity.Book;

public interface IStorageService {
	public Task<AssetFileResult?> GetFileStreamAsync(Guid assetId, CancellationToken cancellationToken = default);

	public Task<AssetModel> UploadAssetAsync(Guid seriesId, Stream content, string contentType, string originalFileName,
		AssetRefType refType, string? prefix = null, CancellationToken cancellationToken = default);

	public Task DeleteFileAsync(Guid assetId, CancellationToken cancellationToken = default);
}
