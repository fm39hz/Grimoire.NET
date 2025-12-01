namespace Grimoire.Application.Dto.Book;

using Common;
using Domain.Entity.Book;

public class SeriesResponseDto(SeriesModel series) : IResponseDto {
	public string Title { get; init; } = series.Title;
	public SeriesMetadata Metadata { get; init; } = series.Metadata;
	public Guid Id { get; init; } = series.Id;
}
