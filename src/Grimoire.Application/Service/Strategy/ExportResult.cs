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
}
