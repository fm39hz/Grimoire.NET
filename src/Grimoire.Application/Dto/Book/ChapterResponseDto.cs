namespace Grimoire.Application.Dto.Book;

using System.Text.Json.Serialization;
using Segment;

public class ChapterResponseDto {
	public string VolumeId { get; init; } = string.Empty;
	public int Order { get; init; }
	public string Title { get; init; } = string.Empty;
	public List<SegmentDto> Content { get; init; } = [];
	public List<FootnoteSegmentDto> Footnotes { get; init; } = [];
	public string Id { get; init; } = string.Empty;

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTime? CreatedAt { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTime? UpdatedAt { get; set; }
}
