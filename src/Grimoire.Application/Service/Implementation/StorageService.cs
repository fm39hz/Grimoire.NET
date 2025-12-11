namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common.Repository;
using Domain.Entity.Book;

public sealed class StorageService(IStorageRepository repository) : IStorageService {
	public async Task<Stream?> GetFileStreamAsync(Guid assetId) =>
		await repository.GetFileStreamAsync(assetId);

	public async Task<AssetModel> UploadAssetAsync(Guid seriesId, Stream content, string contentType,
		string originalFileName, AssetRefType refType) =>
		await repository.UploadAssetAsync(seriesId, content, contentType, originalFileName, refType);

	public async Task DeleteFileAsync(Guid assetId) =>
		await repository.DeleteFileAsync(assetId);
}
