namespace Grimoire.Infrastructure.Export.Common;

using Application.Dto.Book;
using Application.Export;
using Scriban.Runtime;

public class ScribanExportFunctions : ScriptObject {
	public static string GenerateAnchor(string title) =>
		title?.ToLowerInvariant().Replace(" ", "-") ?? string.Empty;

	public static bool ShouldShowDescriptionInIntro(ExportSectionDto? section) =>
		!ExportUtilities.IsSplitDescriptionEnabled(section);

	public static bool IsSectionType(ExportSectionDto section, params string[] types) =>
		types.Contains(section.Type.ToString(), StringComparer.OrdinalIgnoreCase);

	public static bool IsSectionTypeExact(ExportSectionDto section, string targetType) =>
		Enum.TryParse<BookSection>(targetType, true, out var targetEnum) &&
		section.Type == targetEnum;
}
