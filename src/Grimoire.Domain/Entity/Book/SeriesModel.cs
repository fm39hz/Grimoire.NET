namespace Grimoire.Domain.Entity.Book;

using Metadata;

/// <summary>
///     Represents a book series (e.g., a manga series)
/// </summary>
public record SeriesModel : BaseModel {
	public SeriesModel(SeriesModel other) : base(other) {
		Title = other.Title;
		Metadata = other.Metadata;
	}

	/// <summary>
	///     Title of the series
	/// </summary>
	public required string Title { get; set; }

	/// <summary>
	///     Strongly-typed metadata for the series
	/// </summary>
	public SeriesMetadata Metadata { get; set; } = new();
}
