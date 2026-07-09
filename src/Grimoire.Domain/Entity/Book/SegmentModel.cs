namespace Grimoire.Domain.Entity.Book;

using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
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
	///     The hierarchical ltree path for this segment node.
	///     Using Microsoft.EntityFrameworkCore.LTree directly in Domain (pragmatic design)
	///     to leverage native PG LTree operations (LCA, IsDescendantOf, Subpath) both in SQL LINQ
	///     and in-memory tests, avoiding custom C# string parsing workarounds.
	/// </summary>
	public LTree Path { get; set; } = string.Empty;

	/// <summary>
	///     Order of this segment within the chapter
	/// </summary>
	public double Order { get; set; }
}
