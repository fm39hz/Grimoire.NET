namespace Grimoire.Infrastructure.Export.Common;

/// <summary>
///     Format-agnostic navigation entry. PageId is a logical identifier
///     (e.g. chapter.Id.ToString()), never a filename or file path.
/// </summary>
public record NavEntry(
	string PageId,
	string Title,
	IReadOnlyList<NavEntry>? Children = null
	);
