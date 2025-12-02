namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book;
using Domain.Entity.Book.Metadata;

public record SeriesRequestDto(string Title, SeriesMetadata Metadata)
	: IRequestDto<SeriesModel> {
	public SeriesModel ToModel() => new() { Title = Title, Metadata = Metadata };
}
