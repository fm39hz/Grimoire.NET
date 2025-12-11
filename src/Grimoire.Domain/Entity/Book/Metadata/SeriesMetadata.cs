namespace Grimoire.Domain.Entity.Book.Metadata;

using Segment;

/// <summary>
///     Represents the strongly-typed metadata for a book series.
///     This is a value object stored as JSONB, not a separate entity.
/// </summary>
public sealed record SeriesMetadata {
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
	public List<TextSegmentModel> Description { get; init; } = [];

	/// <summary>
	///     Gets or sets the URL of the cover image for the series.
	/// </summary>
	public string CoverImage { get; init; } = string.Empty;
}
