namespace Grimoire.Application.Service.Contract;

using Domain.Entity.Book;

public interface IStorageService {
	public Task<Stream?> GetFileStreamAsync(Guid assetId);

	public Task<AssetModel> UploadAssetAsync(Guid seriesId, Stream content, string contentType, string originalFileName,
		AssetRefType refType);

	public Task DeleteFileAsync(Guid assetId);
}
