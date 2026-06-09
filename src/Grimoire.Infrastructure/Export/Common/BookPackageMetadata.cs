namespace Grimoire.Infrastructure.Export.Common;

using System.Collections.Generic;
using Grimoire.Application.Dto.Book;

public record BookPackageMetadata(
	string Title,
	string? Author = null,
	string? Language = null,
	string? PlainTextDescription = null,
	IReadOnlyList<string>? Tags = null,
	ExportLocalizationDto? Localization = null
	);
