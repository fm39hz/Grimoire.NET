namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book;
using Segment;

public record UpdateChapterRequestDto(
	double? Order,
	string? Title,
	List<SegmentModel>? Content,
	List<FootnoteSegmentDto>? Footnotes,
	string? VolumeId);
