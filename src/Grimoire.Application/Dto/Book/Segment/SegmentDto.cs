namespace Grimoire.Application.Dto.Book.Segment;

using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextSegmentDto), "Text")]
[JsonDerivedType(typeof(ImageSegmentDto), "Image")]
[JsonDerivedType(typeof(DividerSegmentDto), "Divider")]
[JsonDerivedType(typeof(FootnoteSegmentDto), "Footnote")]
public abstract record SegmentDto {
	public Guid Id { get; init; } = Guid.Empty;
}
