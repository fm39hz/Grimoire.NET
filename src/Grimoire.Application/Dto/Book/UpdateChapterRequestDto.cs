namespace Grimoire.Application.Dto.Book;

using Segment;

public record UpdateChapterRequestDto(
	double? Order,
	string? Title,
	List<SegmentDto>? Content,
	List<FootnoteSegmentDto>? Footnotes,
	string? VolumeId);
