namespace Grimoire.Domain.Entity.Book;

/// <summary>
///     Represents a glossary term for a series
/// </summary>
public class GlossaryTerm : BaseModel {
	/// <summary>
	///     Foreign key to the series
	/// </summary>
	public required Guid SeriesId { get; init; }

	/// <summary>
	///     The term
	/// </summary>
	public required string Term { get; set; }

	/// <summary>
	///     Definition of the term
	/// </summary>
	public required string Definition { get; set; }

	/// <summary>
	///     Additional notes
	/// </summary>
	public string? Note { get; set; }

	/// <summary>
	///     Type of term (e.g., "General", "Character", "Location")
	/// </summary>
	public string Type { get; set; } = "General";

	/// <summary>
	///     Navigation property to Series
	/// </summary>
	public SeriesModel? Series { get; set; }
}
