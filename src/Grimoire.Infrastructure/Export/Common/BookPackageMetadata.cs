namespace Grimoire.Infrastructure.Export.Common;

public record BookPackageMetadata(
	string Title,
	string? Author = null,
	string? Language = null,
	string? PlainTextDescription = null,
	IReadOnlyList<string>? Tags = null
	);
