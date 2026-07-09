namespace Grimoire.Domain.Entity.Book;

using Microsoft.EntityFrameworkCore;

using Metadata;

/// <summary>
///     Represents a book series (e.g., a manga series)
/// </summary>
public class SeriesModel : BaseModel {
	/// <summary>
	///     Title of the series
	/// </summary>
	public required string Title { get; set; }

	/// <summary>
	///     The hierarchical ltree path for this series node.
	///     Using Microsoft.EntityFrameworkCore.LTree directly in Domain (pragmatic design)
	///     to leverage native PG LTree operations (LCA, IsDescendantOf, Subpath) both in SQL LINQ
	///     and in-memory tests, avoiding custom C# string parsing workarounds.
	/// </summary>
	public LTree Path { get; set; } = string.Empty;

	/// <summary>
	///     Strongly-typed metadata for the series
	/// </summary>
	public SeriesMetadata Metadata { get; set; } = new();

	/// <summary>
	///     Collection of glossary terms for this series
	/// </summary>
	public ICollection<GlossaryTerm> GlossaryTerms { get; init; } = [];

	/// <summary>
	///     Collection of source materials for this series
	/// </summary>
	public ICollection<SourceMaterial> SourceMaterials { get; init; } = [];
}
