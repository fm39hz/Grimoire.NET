namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book;
using System.Collections.Generic;

public record UpdateChapterRequestDto(
	float? Order,
	string? Title,
	ICollection<CreateChapterVariantDto>? Variants);
