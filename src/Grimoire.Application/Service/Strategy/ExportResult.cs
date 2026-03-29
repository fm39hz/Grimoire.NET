namespace Grimoire.Application.Service.Strategy;

/// <summary>
///     Result of an export operation
/// </summary>
public class ExportResult {
	public required Stream ContentStream { get; init; }
	public required string FileName { get; init; }
	public required string ContentType { get; init; }
	public bool Success { get; init; } = true;
	public string? ErrorMessage { get; init; }

	public static ExportResult Ok(Stream stream, string fileName, string contentType) => new() {
		ContentStream = stream,
		FileName = fileName,
		ContentType = contentType,
		Success = true
	};

	public static ExportResult Fail(string error) => new() {
		ContentStream = Stream.Null,
		FileName = string.Empty,
		ContentType = string.Empty,
		Success = false,
		ErrorMessage = error
	};
}
