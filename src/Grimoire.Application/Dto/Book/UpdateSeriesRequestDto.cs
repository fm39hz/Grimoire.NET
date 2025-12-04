namespace Grimoire.Application.Dto.Book;

using Metadata;

public record UpdateSeriesRequestDto(string? Title, SeriesMetadataDto? Metadata);
