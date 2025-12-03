namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book;

public record UpdateChapterRequestDto(
	int? Order,
	string? Title,
	List<SegmentModel>? Content,
	List<ImportFootnoteDto>? Footnotes);
