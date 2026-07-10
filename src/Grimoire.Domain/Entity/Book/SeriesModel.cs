namespace Grimoire.Domain.Entity.Book;

using Common.ValueObject;
using Metadata;
using Microsoft.EntityFrameworkCore;

/// <summary>
///     Represents a book series (e.g., a manga series)
/// </summary>
public class SeriesModel : BaseModel {

	/// <summary>
	///     Title of the series
	/// </summary>
	public required string Title { get; set; }

	/// <summary>
	///     The hierarchical path for this series node in the domain layer.
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
