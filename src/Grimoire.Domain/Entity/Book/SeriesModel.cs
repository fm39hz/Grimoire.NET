namespace Grimoire.Domain.Entity.Book;

using Metadata;
using Microsoft.EntityFrameworkCore;

/// <summary>
///     Represents a book series (e.g., a manga series)
/// </summary>
[Index(nameof(Slug), IsUnique = true)]
public record SeriesModel : BaseModel {
	public SeriesModel(SeriesModel other) : base(other) {
		Title = other.Title;
		Slug = other.Slug;
		Metadata = other.Metadata;
		Volumes = other.Volumes;
		Assets = other.Assets;
	}

	/// <summary>
	///     Title of the series
	/// </summary>
	public required string Title { get; set; }

	/// <summary>
	///     URL-friendly identifier for the series.
	/// </summary>
	public required string Slug { get; set; }

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
