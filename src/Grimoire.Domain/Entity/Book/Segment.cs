namespace Grimoire.Domain.Entity.Book;

using System.Text.Json.Serialization;

/// <summary>
///     Base class for all content segments within a chapter.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextSegment), nameof(TextSegment))]
[JsonDerivedType(typeof(ImageSegment), nameof(ImageSegment))]
[JsonDerivedType(typeof(DividerSegment), nameof(DividerSegment))]
public abstract record Segment : BaseModel;
