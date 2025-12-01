namespace Grimoire.Domain.Entity.Book;

/// <summary>
///     Represents a chapter within a volume
/// </summary>
public record ChapterModel : BaseModel {
	/// <summary>
	///     Foreign key to the volume
	/// </summary>
	public required Guid VolumeId { get; init; }

	/// <summary>
	///     Reference to the parent volume
	/// </summary>
	public VolumeModel? Volume { get; init; }

	/// <summary>
	///     Order of this chapter within the volume
	/// </summary>
	public int Order { get; init; }

	/// <summary>
	///     Title of the chapter
	/// </summary>
	public required string Title { get; init; }

	/// <summary>
	///     Content of the chapter, composed of various segments.
	/// </summary>
	public List<Segment> Content { get; init; } = [];
}
