namespace Grimoire.Application.Dto.Book;

using Segment;

public record CreateChapterRequestDto(
	string VolumeId,
	double Order,
	string Title,
	List<SegmentDto>? Content,
	List<ImportFootnoteDto>? Footnotes,
	string? RawContent);
