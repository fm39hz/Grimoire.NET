namespace Grimoire.Domain.Entity.Book;

using System.Text.Json.Serialization;
using Common.ValueObject;
using Microsoft.EntityFrameworkCore;
using Segment;

/// <summary>
///     Base class for all content segments within a chapter.
///     Segments are value objects stored in JSONB, not separate entities.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextSegmentModel), "Text")]
[JsonDerivedType(typeof(ImageSegmentModel), "Image")]
[JsonDerivedType(typeof(DividerSegmentModel), "Divider")]
[JsonDerivedType(typeof(FootnoteSegmentModel), "Footnote")]
public abstract class SegmentModel : BaseModel {

	/// <summary>
	///     The hierarchical path for this segment node in the domain layer.
	///     Not JSON-serialized: it is derived from the persisted <see cref="DbPath"/>, and
	///     segments embedded in JSONB columns (description / footnotes) are not part of the
	///     persisted node tree, so they carry no meaningful path.
	/// </summary>
	[JsonIgnore]
	public BookPath Path {
		get => new(DbPath.ToString());
		set => DbPath = (LTree)value.Value;
	}

	/// <summary>
	///     The database-mapped LTree path. Used by EF Core and Repository queries.
	///     Not JSON-serialized: System.Text.Json cannot serialize the Npgsql <see cref="LTree"/>
	///     type (its <c>NLevel</c> getter has no client-side translation), and EF's value comparer
	///     snapshots JSONB columns containing segments via <c>JsonSerializer</c>.
	/// </summary>
	[JsonIgnore]
	public LTree DbPath { get; set; } = string.Empty;

	/// <summary>
	///     Order of this segment within the chapter
	/// </summary>
	public double Order { get; set; }
}
