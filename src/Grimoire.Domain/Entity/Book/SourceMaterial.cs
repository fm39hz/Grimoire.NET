namespace Grimoire.Domain.Entity.Book;

/// <summary>
///     Represents normalized input text (Markdown) for a Series
/// </summary>
public class SourceMaterial : BaseModel {
	/// <summary>
	///     Foreign key to the series
	/// </summary>
	public required Guid SeriesId { get; init; }

	/// <summary>
	///     Title of the source material
	/// </summary>
	public required string Title { get; set; }

	/// <summary>
	///     Normalized Markdown content
	/// </summary>
	public required string MarkdownContent { get; set; }

	/// <summary>
	///     Optional URL where this content was sourced from
	/// </summary>
	public string? SourceUrl { get; set; }

	/// <summary>
	///     Navigation property to Series
	/// </summary>
	public SeriesModel? Series { get; set; }
}
