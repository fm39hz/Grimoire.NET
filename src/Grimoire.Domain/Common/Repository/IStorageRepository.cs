namespace Grimoire.Domain.Common.Repository;

public interface IStorageRepository {
	public Task<string> SaveFileAsync(string relativePath, Stream content, string contentType);
	public Task<byte[]> GetFileAsync(string relativePath);
	public Task DeleteFileAsync(string relativePath);
}
