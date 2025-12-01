namespace Grimoire.Domain.Entity.Book;

/// <summary>
///     Represents a volume within a series
/// </summary>
public record VolumeModel : BaseModel {
	/// <summary>
	///     Foreign key to the series
	/// </summary>
	public required Guid SeriesId { get; init; }

	/// <summary>
	///     Reference to the parent series
	/// </summary>
	public SeriesModel? Series { get; init; }

	/// <summary>
	///     Order of this volume within the series
	/// </summary>
	public int Order { get; init; }

	/// <summary>
	///     Title of the volume
	/// </summary>
	public required string Title { get; init; }

	/// <summary>
	///     Strongly-typed metadata for the volume
	/// </summary>
	public VolumeMetadata Metadata { get; init; } = new();

	/// <summary>
	///     Collection of chapters in this volume
	/// </summary>
	public ICollection<ChapterModel> Chapters { get; init; } = new List<ChapterModel>();
}
