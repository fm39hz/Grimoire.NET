namespace Grimoire.Infrastructure.Export.Common;

/// <summary>
///     Constants used throughout EPUB export process
/// </summary>
public static class EpubConstants {
	/// <summary>
	///     Localized text content (Vietnamese)
	/// </summary>
	public static class LocalizedText {
		public const string INTRODUCTION = "Giới thiệu";
		public const string SUMMARY = "Tóm tắt";
		public const string TABLE_OF_CONTENTS = "Mục lục";
		public const string FOOTNOTE = "Ghi chú:";
		public const string AUTHOR = "Tác giả: ";
		public const string PUBLICATION_DATE = "Ngày xuất bản: ";
	}

	/// <summary>
	///     EPUB file paths
	/// </summary>
	public static class Paths {
		public const string OEBPS_PREFIX = "OEBPS/";
		public const string IMAGES_FOLDER = "images/";
		public const string NAV_FILE = "OEBPS/nav.xhtml";
		public const string TOC_NCX_FILE = "OEBPS/toc.ncx";
		public const string CONTENT_OPF_FILE = "OEBPS/content.opf";
		public const string STYLE_CSS_FILE = "OEBPS/style.css";
		public const string CONTAINER_XML_FILE = "META-INF/container.xml";
		public const string MIME_TYPE_FILE = "mimetype";
	}

	/// <summary>
	///     EPUB metadata defaults
	/// </summary>
	public static class Defaults {
		public const string LANGUAGE = "vi";
		public const string IMAGE_EXTENSION = ".jpg";
		public const string UNTITLED_BOOK = "Untitled";
	}
}
