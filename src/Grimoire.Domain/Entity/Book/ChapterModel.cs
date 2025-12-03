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
	public float Order { get; set; }

	/// <summary>
	///     Title of the chapter
	/// </summary>
	public required string Title { get; set; }

	/// <summary>
	///     Collection of variants for this chapter.
	/// </summary>
	public ICollection<ChapterVariantModel> Variants { get; init; } = [];
}
