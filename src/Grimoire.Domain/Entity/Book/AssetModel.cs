namespace Grimoire.Domain.Entity.Book;

/// <summary>
///     Represents an asset (file) stored in S3 Compatible
/// </summary>
public class AssetModel : BaseModel {
	/// <summary>
	///     Foreign key to the series this asset belongs to
	/// </summary>
	public required Guid SeriesId { get; init; }

	/// <summary>
	///     Reference to the parent series
	/// </summary>
	public SeriesModel? Series { get; init; }

	/// <summary>
	///     S3 path/key for the asset
	/// </summary>
	public required string Path { get; init; }

	/// <summary>
	///     MD5/SHA256 hash of the file for deduplication
	/// </summary>
	public required string FileHash { get; init; }

	/// <summary>
	///     Type of reference: Cover or Content
	/// </summary>
	public required AssetRefType RefType { get; init; }

	/// <summary>
	///     Original MIME type of the uploaded file (e.g. image/jpeg)
	/// </summary>
	public required string ContentType { get; init; }

	/// <summary>
	///     Original file name at upload time
	/// </summary>
	public required string OriginalFileName { get; init; }
}
