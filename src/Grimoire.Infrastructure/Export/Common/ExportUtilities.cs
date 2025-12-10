namespace Grimoire.Infrastructure.Export.Common;

using Application.Dto.Book;

/// <summary>
///     Provides common utilities for export strategies
/// </summary>
public static class ExportUtilities {
	/// <summary>
	///     Sanitizes a filename by removing invalid characters
	/// </summary>
	public static string SanitizeFileName(string fileName) {
		var invalidChars = Path.GetInvalidFileNameChars();
		return string.Join("_", fileName.ToLowerInvariant().Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
	}

	/// <summary>
	///     Checks if the section option "splitDescription" is enabled
	/// </summary>
	public static bool IsSplitDescriptionEnabled(ExportSectionDto? section) =>
		section?.Options?.TryGetValue("splitDescription", out var splitVal) == true
		&& splitVal is bool split && split;
}
