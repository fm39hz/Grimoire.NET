namespace Grimoire.Domain.Entity.Book;

/// <summary>
///     Represents the strongly-typed metadata for a book series.
/// </summary>
public sealed record SeriesMetadata : BaseModel {
	/// <summary>
	///     Gets or sets the authors of the series.
	/// </summary>
	public ICollection<string> Authors { get; init; } = [];

	/// <summary>
	///     Gets or sets the artists of the series.
	/// </summary>
	public ICollection<string> Artists { get; init; } = [];

	/// <summary>
	///     Gets or sets the tags associated with the series.
	/// </summary>
	public ICollection<string> Tags { get; init; } = [];

	/// <summary>
	///     Gets or sets the description of the series.
	/// </summary>
	public string Description { get; init; } = string.Empty;

	/// <summary>
	///     Gets or sets the URL of the cover image for the series.
	/// </summary>
	public string CoverImageUrl { get; init; } = string.Empty;
}
