namespace Grimoire.Application.Dto.Book.Metadata;

public class VolumeMetadataDto {
	/// <summary>
	///     Gets or sets the URL of the cover image for the volume
	/// </summary>
	public string? CoverImage { get; init; }

	/// <summary>
	///     Gets or sets the publication date of the volume.
	/// </summary>
	public DateTime? PublicationDate { get; init; }

	/// <summary>
	///     Gets or sets the ISBN of the volume.
	/// </summary>
	public string? Isbn { get; init; }
}
