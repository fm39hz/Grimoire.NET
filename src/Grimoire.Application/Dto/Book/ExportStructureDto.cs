namespace Grimoire.Application.Dto.Book;

using Export;

/// <summary>
///     Defines a section in the export structure
/// </summary>
public record ExportSectionDto {
	/// <summary>
	///     Type of section (e.g., "IntroPage", "TOC", "Content", "Description")
	/// </summary>
	public BookSection Type { get; init; }

	/// <summary>
	///     Custom CSS to apply to this section
	/// </summary>
	public string? CustomCss { get; init; }

	/// <summary>
	///     Additional options for this section
	/// </summary>
	public Dictionary<string, object>? Options { get; init; }
}

/// <summary>
///     Defines the overall structure/layout of the export
/// </summary>
public record ExportStructureDto {
	/// <summary>
	///     Ordered list of sections that make up the document structure
	///     Example: ["IntroPage", "TOC", "Content"] or ["IntroPage", "Content", "TOC"]
	/// </summary>
	public List<ExportSectionDto> Sections { get; init; } = [];

	/// <summary>
	///     Global CSS to apply to the entire document
	/// </summary>
	public string? GlobalCss { get; init; }

	/// <summary>
	///     Controls where footnotes are rendered: inline per-chapter, consolidated per-volume, or global.
	/// </summary>
	public FootnoteMode FootnoteMode { get; init; } = FootnoteMode.PerVolume;

	/// <summary>
	///     Controls how footnotes are grouped within consolidated endnotes pages.
	/// </summary>
	public EndnoteGrouping EndnoteGrouping { get; init; } = EndnoteGrouping.ByChapter;

	/// <summary>
	///     Style of the footnote markers (Parentheses, SquareBrackets, Asterisk, SuperScript).
	/// </summary>
	public FootnoteStyle FootnoteStyle { get; init; } = FootnoteStyle.Parentheses;

	/// <summary>
	///     Whether to enable a dropcap at the first text segment of a chapter.
	/// </summary>
	public bool EnableDropcap { get; init; } = false;

	/// <summary>
	///     Localization settings for labels and language metadata.
	/// </summary>
	public ExportLocalizationDto Localization { get; init; } = new();
}
