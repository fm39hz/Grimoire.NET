namespace Grimoire.Domain.Entity.Book;

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
public abstract record SegmentModel {
	/// <summary>
	///     Unique identifier for the segment (used for footnote references)
	/// </summary>
	public Guid Id { get; init; } = Guid.CreateVersion7();
}
