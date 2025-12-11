namespace Grimoire.Application.Service.Contract;

using Domain.Entity.Book;

public interface IStorageService {
	Task<Stream?> GetFileStreamAsync(Guid assetId);
	Task<AssetModel> UploadAssetAsync(Guid seriesId, Stream content, string contentType, string originalFileName, string refType);
	Task DeleteFileAsync(Guid assetId);
}
