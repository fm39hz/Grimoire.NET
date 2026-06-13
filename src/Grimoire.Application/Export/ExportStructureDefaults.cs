namespace Grimoire.Application.Export;

using Dto.Book;

public static class ExportStructureDefaults {
	public const string SplitDescriptionOptionKey = "splitDescription";

	public static ExportStructureDto Standard() => new() {
		Sections = [
			new ExportSectionDto {
				Type = BookSection.IntroPage,
				Options = new Dictionary<string, object> { { SplitDescriptionOptionKey, false } }
			},
			new ExportSectionDto { Type = BookSection.Description },
			new ExportSectionDto { Type = BookSection.Toc },
			new ExportSectionDto { Type = BookSection.Content }
		]
	};
}
