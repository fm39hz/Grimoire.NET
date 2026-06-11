namespace Grimoire.Application.Dto.Book.Metadata;

using Segment;

public class SeriesMetadataDto {
	/// <summary>
	///     Gets or sets the authors of the series.
	/// </summary>
	public ICollection<string>? Authors { get; init; }

	/// <summary>
	///     Gets or sets the artists of the series.
	/// </summary>
	public ICollection<string>? Artists { get; init; }

	/// <summary>
	///     Gets or sets the tags associated with the series.
	/// </summary>
	public ICollection<string>? Tags { get; init; }

	/// <summary>
	///     Gets or sets the description of the series.
	/// </summary>
	public List<TextSegmentDto>? Description { get; init; }

	/// <summary>
	///     Gets or sets the URL of the cover image for the series.
	/// </summary>
	public string? CoverImage { get; set; }
}
