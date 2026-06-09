namespace Grimoire.Application.Service.Implementation;

using System.Threading;
using Contract;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;

public sealed class StorageService(IStorageRepository repository) : IStorageService {
	public async Task<AssetFileResult?> GetFileStreamAsync(Guid assetId, CancellationToken cancellationToken = default) =>
		await repository.GetFileStreamAsync(assetId, cancellationToken);

	public async Task<AssetModel> UploadAssetAsync(Guid seriesId, Stream content, string contentType,
		string originalFileName, AssetRefType refType, string? prefix = null,
		CancellationToken cancellationToken = default) =>
		await repository.UploadAssetAsync(seriesId, content, contentType, originalFileName, refType, prefix, cancellationToken);

	public async Task DeleteFileAsync(Guid assetId, CancellationToken cancellationToken = default) =>
		await repository.DeleteFileAsync(assetId, cancellationToken);
}
