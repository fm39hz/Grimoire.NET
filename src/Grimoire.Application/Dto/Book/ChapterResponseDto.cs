namespace Grimoire.Application.Dto.Book;

using Segment;

public class ChapterResponseDto {
	public Guid VolumeId { get; init; } = Guid.Empty;
	public int Order { get; init; }
	public string Title { get; init; } = string.Empty;
	public List<SegmentDto> Content { get; init; } = [];
	public List<FootnoteSegmentDto> Footnotes { get; init; } = [];
	public Guid Id { get; init; } = Guid.Empty;
}
