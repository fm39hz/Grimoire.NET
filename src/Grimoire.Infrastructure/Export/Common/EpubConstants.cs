namespace Grimoire.Infrastructure.Export.Common;

/// <summary>
///     Constants used throughout EPUB export process
/// </summary>
public static class EpubConstants {
	/// <summary>
	///     Section type identifiers (case-insensitive)
	/// </summary>
	public static class SectionTypes {
		public const string IntroPage = "intropage";
		public const string Intro = "intro";
		public const string Description = "description";
		public const string Toc = "toc";
		public const string TableOfContents = "tableofcontents";
		public const string Content = "content";
		public const string Chapters = "chapters";
	}

	/// <summary>
	///     Section option keys
	/// </summary>
	public static class SectionOptions {
		public const string SplitDescription = "splitDescription";
	}

	/// <summary>
	///     Localized text content (Vietnamese)
	/// </summary>
	public static class LocalizedText {
		public const string Introduction = "Giới thiệu";
		public const string Summary = "Tóm tắt";
		public const string TableOfContents = "Mục lục";
		public const string Footnote = "Ghi chú:";
		public const string Author = "Tác giả: ";
	}

	/// <summary>
	///     EPUB file paths
	/// </summary>
	public static class Paths {
		public const string OebpsPrefix = "OEBPS/";
		public const string ImagesFolder = "images/";
		public const string NavFile = "OEBPS/nav.xhtml";
		public const string TocNcxFile = "OEBPS/toc.ncx";
		public const string ContentOpfFile = "OEBPS/content.opf";
		public const string StyleCssFile = "OEBPS/style.css";
		public const string ContainerXmlFile = "META-INF/container.xml";
		public const string MimeTypeFile = "mimetype";
	}

	/// <summary>
	///     EPUB metadata defaults
	/// </summary>
	public static class Defaults {
		public const string Language = "vi";
		public const string ImageExtension = ".jpg";
		public const string UntitledBook = "Untitled";
	}
}
