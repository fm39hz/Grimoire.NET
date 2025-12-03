namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book;
using System.Collections.Generic;

public record CreateChapterRequestDto(
	Guid VolumeId,
	float Order,
	string Title,
	ICollection<CreateChapterVariantDto> Variants) : IRequestDto<ChapterModel> {
	public ChapterModel ToModel() => throw new NotImplementedException("Use Service.Create to handle ID normalization");
}
