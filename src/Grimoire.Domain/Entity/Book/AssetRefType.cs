namespace Grimoire.Domain.Entity.Book;

/// <summary>
///     Enum for asset reference types
/// </summary>
public enum AssetRefType {
	/// <summary>
	///     Asset is used as a cover image
	/// </summary>
	Cover,

	/// <summary>
	///     Asset is used as content (inline images, etc.)
	/// </summary>
	Content,

	/// <summary>
	///     Asset is an export result (EPUB, PDF, etc.)
	/// </summary>
	Export
}
