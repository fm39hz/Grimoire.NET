namespace Grimoire.Application.Export;

using System.Collections.Generic;
using Dto.Book;

public record BookPackageMetadata(
	string Title,
	string? Author = null,
	string? Language = null,
	string? PlainTextDescription = null,
	IReadOnlyList<string>? Tags = null,
	ExportLocalizationDto? Localization = null
	);
