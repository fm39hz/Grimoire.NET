namespace Grimoire.Domain.Entity.Book;

using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Segment;
using Common.ValueObject;

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
	private LTree _dbPath = string.Empty;

	/// <summary>
	///     The hierarchical path for this segment node in the domain layer.
	/// </summary>
	public BookPath Path {
		get => new(_dbPath.ToString());
		set => _dbPath = (LTree)value.Value;
	}

	/// <summary>
	///     The database-mapped LTree path. Used by EF Core and Repository queries.
	/// </summary>
	public LTree DbPath {
		get => _dbPath;
		set => _dbPath = value;
	}

	/// <summary>
	///     Order of this segment within the chapter
	/// </summary>
	public double Order { get; set; }
}
