namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book;

public record CreateChapterRequestDto(
	Guid VolumeId,
	int Order,
	string Title,
	List<SegmentModel> Content,
	List<ImportFootnoteDto> Footnotes);
