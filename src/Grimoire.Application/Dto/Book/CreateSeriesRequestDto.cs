namespace Grimoire.Application.Dto.Book;

using Metadata;

public record CreateSeriesRequestDto(string Title, SeriesMetadataDto Metadata);
