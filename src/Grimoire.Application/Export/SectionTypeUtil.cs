namespace Grimoire.Application.Export;

public static class SectionTypeUtil {
	public static BookSection FromString(string sectionType) => sectionType.ToLowerInvariant() switch {
		"intro" => BookSection.Intro,
		"intropage" => BookSection.IntroPage,
		"description" => BookSection.Description,
		"content" => BookSection.Content,
		"chapters" => BookSection.Chapters,
		"toc" => BookSection.Toc,
		"tableofcontents" => BookSection.TableOfContents,
		_ => BookSection.Unknown
	};
}
