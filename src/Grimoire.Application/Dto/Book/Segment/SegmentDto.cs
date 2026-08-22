namespace Grimoire.Application.Dto.Book.Segment;

using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextSegmentDto), "Text")]
[JsonDerivedType(typeof(ImageSegmentDto), "Image")]
[JsonDerivedType(typeof(DividerSegmentDto), "Divider")]
[JsonDerivedType(typeof(FootnoteSegmentDto), "Footnote")]
[JsonDerivedType(typeof(TableSegmentDto), "Table")]
public abstract record SegmentDto {
	public string Id { get; init; } = string.Empty;
}
