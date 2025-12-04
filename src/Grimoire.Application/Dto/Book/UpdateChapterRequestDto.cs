namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book;
using System.Collections.Generic;

public record UpdateChapterRequestDto(
	int? Order,
	string? Title,
	List<SegmentModel>? Content,
	List<ImportFootnoteDto>? Footnotes);
