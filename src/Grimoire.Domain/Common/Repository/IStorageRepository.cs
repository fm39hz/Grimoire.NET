namespace Grimoire.Domain.Common.Repository;

using Entity.Book;

public interface IStorageRepository {
	public Task<AssetModel> UploadAssetAsync(Guid seriesId, Stream content, string contentType, string originalFileName,
		string refType);

	public Task<byte[]> GetFileAsync(Guid assetId);
	public Task DeleteFileAsync(Guid assetId);
}
