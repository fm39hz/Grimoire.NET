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
	///     Order of this chapter within the volume
	/// </summary>
	public float Order { get; set; }

	/// <summary>
	///     Title of the chapter
	/// </summary>
	public required string Title { get; set; }

	/// <summary>
	///     Status of the chapter
	/// </summary>
	public ChapterStatus Status { get; set; } = ChapterStatus.Draft;

	/// <summary>
	///     Navigation property to chapter content (1-1 relationship)
	/// </summary>
	public ChapterContentModel? ContentData { get; set; }
}
