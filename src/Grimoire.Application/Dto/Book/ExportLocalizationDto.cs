namespace Grimoire.Application.Dto.Book;

public record ExportLocalizationDto {
	/// <summary>Language code, e.g. "vi", "en"</summary>
	public string Language { get; init; } = "vi";

	/// <summary>Label for introduction section</summary>
	public string IntroductionLabel { get; init; } = "Giới thiệu";

	/// <summary>Label for summary / description section</summary>
	public string SummaryLabel { get; init; } = "Tóm tắt";

	/// <summary>Label for table of contents</summary>
	public string TableOfContentsLabel { get; init; } = "Mục lục";

	/// <summary>Label prefix for footnotes list</summary>
	public string FootnoteLabel { get; init; } = "Ghi chú:";

	/// <summary>Label prefix for author field</summary>
	public string AuthorLabel { get; init; } = "Tác giả:";

	/// <summary>Label prefix for publication date</summary>
	public string PublicationDateLabel { get; init; } = "Ngày xuất bản:";
}
