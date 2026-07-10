namespace Grimoire.Domain.Entity.Book;

using Common.ValueObject;
using Metadata;
using Microsoft.EntityFrameworkCore;

/// <summary>
///     Represents a volume within a series
/// </summary>
public class VolumeModel : BaseModel {

	/// <summary>
	///     The hierarchical path for this volume node in the domain layer.
	/// </summary>
	public BookPath Path {
		get => new(DbPath.ToString());
		set => DbPath = (LTree)value.Value;
	}

	/// <summary>
	///     The database-mapped LTree path. Used by EF Core and Repository queries.
	/// </summary>
	public LTree DbPath { get; set; } = string.Empty;

	/// <summary>
	///     Order of this volume within the series
	/// </summary>
	public double Order { get; set; }

	/// <summary>
	///     Title of the volume
	/// </summary>
	public required string Title { get; set; }

	/// <summary>
	///     Strongly-typed metadata for the volume
	/// </summary>
	public VolumeMetadata? Metadata { get; set; }
}
