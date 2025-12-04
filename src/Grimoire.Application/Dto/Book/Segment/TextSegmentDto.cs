namespace Grimoire.Application.Dto.Book.Segment;

using System.Text.Json.Serialization;

public sealed record TextSegmentDto : SegmentDto {
	public ICollection<TextRunDto> Runs { get; init; } = [];
}

public sealed record TextRunDto(
	string Text,
	[property : JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	bool IsBold = false,
	[property : JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	bool IsItalic = false,
	[property : JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	string? FootnoteId = null
	);
