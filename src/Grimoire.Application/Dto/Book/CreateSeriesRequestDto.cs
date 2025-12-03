namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book;
using Domain.Entity.Book.Metadata;
using System;

public record CreateSeriesRequestDto(string Title, SeriesMetadata Metadata)
	: IRequestDto<SeriesModel> {
	public SeriesModel ToModel() => throw new NotImplementedException("Use Service.Create to handle slug generation");
}
