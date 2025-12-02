namespace Grimoire.Domain.Entity.Book.Metadata;

/// <summary>
///     Represents the strongly-typed metadata for a volume within a series.
/// </summary>
public sealed record VolumeMetadata : BaseModel {
	/// <summary>
	///     Gets or sets the authors of the volume.
	/// </summary>
	public ICollection<string> Authors { get; init; } = [];

	/// <summary>
	///     Gets or sets the artists of the volume.
	/// </summary>
	public ICollection<string> Artists { get; init; } = [];

	/// <summary>
	///     Gets or sets the publication date of the volume.
	/// </summary>
	public DateTime PublicationDate { get; init; }

	/// <summary>
	///     Gets or sets the ISBN of the volume.
	/// </summary>
	public string Isbn { get; init; } = string.Empty;
}
