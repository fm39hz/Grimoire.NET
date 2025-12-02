namespace Grimoire.Domain.Entity.Book;

using System.Text.Json.Serialization;
using Segment;

/// <summary>
///     Base class for all content segments within a chapter.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextSegmentModel), nameof(TextSegmentModel))]
[JsonDerivedType(typeof(ImageSegmentModel), nameof(ImageSegmentModel))]
[JsonDerivedType(typeof(DividerSegmentModel), nameof(DividerSegmentModel))]
public abstract record SegmentModel : BaseModel;
