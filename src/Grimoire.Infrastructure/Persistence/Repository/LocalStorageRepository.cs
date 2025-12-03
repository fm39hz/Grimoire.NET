namespace Grimoire.Infrastructure.Persistence.Repository;

using Domain.Common.Repository;
using Microsoft.Extensions.Logging;

public partial class LocalStorageRepository(ILogger<LocalStorageRepository> logger) : IStorageRepository {
	private readonly string _storagePath = Path.Combine(Path.GetTempPath(), "grimoire-files");

	public Task<string> SaveFileAsync(string relativePath, Stream content, string contentType) {
		var filePath = Path.Combine(_storagePath, relativePath);
		var directory = Path.GetDirectoryName(filePath);
		if (directory is not null) {
			Directory.CreateDirectory(directory);
		}

		LogSavingFileToFilepath(logger, filePath);

		using var fileStream = new FileStream(filePath, FileMode.Create);
		content.CopyTo(fileStream);

		return Task.FromResult(relativePath);
	}

	public Task<byte[]> GetFileAsync(string relativePath) {
		var filePath = Path.Combine(_storagePath, relativePath);
		LogGettingFileFromFilepath(logger, filePath);
		return !File.Exists(filePath)? Task.FromResult(Array.Empty<byte>()) : File.ReadAllBytesAsync(filePath);
	}

	public Task DeleteFileAsync(string relativePath) {
		var filePath = Path.Combine(_storagePath, relativePath);
		LogDeletingFileFromFilepath(logger, filePath);
		if (File.Exists(filePath)) {
			File.Delete(filePath);
		}

		return Task.CompletedTask;
	}

	[LoggerMessage(LogLevel.Information, "Saving file to {filePath}")]
	static partial void LogSavingFileToFilepath(ILogger<LocalStorageRepository> logger, string filePath);

	[LoggerMessage(LogLevel.Information, "Getting file from {filePath}")]
	static partial void LogGettingFileFromFilepath(ILogger<LocalStorageRepository> logger, string filePath);

	[LoggerMessage(LogLevel.Information, "Deleting file from {filePath}")]
	static partial void LogDeletingFileFromFilepath(ILogger<LocalStorageRepository> logger, string filePath);
}
