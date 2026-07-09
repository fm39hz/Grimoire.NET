namespace Grimoire.Domain.Entity.Book;

using Microsoft.EntityFrameworkCore;

using Metadata;

/// <summary>
///     Represents a volume within a series
/// </summary>
public class VolumeModel : BaseModel {
	/// <summary>
	///     The hierarchical ltree path for this volume node.
	///     Using Microsoft.EntityFrameworkCore.LTree directly in Domain (pragmatic design)
	///     to leverage native PG LTree operations (LCA, IsDescendantOf, Subpath) both in SQL LINQ
	///     and in-memory tests, avoiding custom C# string parsing workarounds.
	/// </summary>
	public LTree Path { get; set; } = string.Empty;

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
