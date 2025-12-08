namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book;

public record CreateChapterRequestDto(
	string VolumeId,
	int Order,
	string Title,
	List<SegmentModel>? Content,
	List<ImportFootnoteDto>? Footnotes,
	string? RawContent);
