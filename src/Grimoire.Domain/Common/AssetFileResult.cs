namespace Grimoire.Domain.Common;

public sealed class AssetFileResult {
	public required Stream Stream { get; init; }
	public required string ContentType { get; init; }
	public required string FileName { get; init; }
}
