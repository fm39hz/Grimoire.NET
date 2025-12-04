namespace Grimoire.Application.Dto.Book.Segment;

public record FootnoteSegmentDto : SegmentDto {
	public List<TextSegmentDto> Segments { get; init; } = [];
}
