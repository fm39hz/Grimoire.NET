namespace Grimoire.Domain.Entity.Book;

using System.Text.Json.Serialization;
using Segment;

/// <summary>
///     Base class for all content segments within a chapter.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextSegmentModel), "Text")]
[JsonDerivedType(typeof(ImageSegmentModel), "Image")]
[JsonDerivedType(typeof(DividerSegmentModel), "Divider")]
[JsonDerivedType(typeof(FootnoteSegmentModel), "Footnote")]
public abstract record SegmentModel : BaseModel;
