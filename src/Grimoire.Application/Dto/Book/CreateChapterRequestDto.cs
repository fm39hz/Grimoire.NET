namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book;
using System.Collections.Generic;

public record CreateChapterRequestDto(
	Guid VolumeId,
	int Order,
	string Title,
	List<SegmentModel> Content,
	List<ImportFootnoteDto> Footnotes) : IRequestDto<ChapterModel> {
	public ChapterModel ToModel() => throw new NotImplementedException("Use Service.Create to handle ID normalization");
}
