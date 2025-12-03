namespace Grimoire.Domain.Entity.Book;

using Metadata;

/// <summary>
///     Represents a book series (e.g., a manga series)
/// </summary>
public record SeriesModel : BaseModel {
	public SeriesModel(SeriesModel other) : base(other) {
		Title = other.Title;
		Metadata = other.Metadata;
		Volumes = other.Volumes;
		Assets = other.Assets;
	}

	/// <summary>
	///     Title of the series
	/// </summary>
	public required string Title { get; set; }

	/// <summary>
	///     Strongly-typed metadata for the series
	/// </summary>
	public SeriesMetadata Metadata { get; set; } = new();

	/// <summary>
	///     Collection of volumes in this series
	/// </summary>
	public ICollection<VolumeModel> Volumes { get; init; } = [];

	/// <summary>
	///     Collection of assets associated with this series
	/// </summary>
	public ICollection<AssetModel> Assets { get; init; } = [];
}
