namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book.Metadata;

public record UpdateSeriesRequestDto(string? Title, SeriesMetadata? Metadata);
