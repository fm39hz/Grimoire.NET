namespace Grimoire.Application.Export;

using System.Collections.Generic;
using Dto.Book;

public record BookPackageMetadata(
	string Title,
	string? Author = null,
	string? Language = null,
	string? PlainTextDescription = null,
	IReadOnlyList<string>? Tags = null,
	ExportLocalizationDto? Localization = null,
	/// <summary>
	///     ISBN of the package's representative volume (Single mode), or null when
	///     the package has no single-volume identity (Anthology). Emitted into the
	///     OPF <c>dc:identifier</c>; also seeds the stable package <c>uid</c>.
	/// </summary>
	string? Isbn = null,
	/// <summary>
	///     Series id used to derive a stable package <c>uid</c> when no ISBN is
	///     available, so re-exporting the same series yields the same identifier
	///     instead of a fresh random GUID.
	/// </summary>
	Guid? SeriesId = null
	);
