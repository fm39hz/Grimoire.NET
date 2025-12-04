namespace Grimoire.Application.Dto.Book;

using Metadata;

public class SeriesResponseDto {
	public string Title { get; init; } = string.Empty;
	public SeriesMetadataDto Metadata { get; init; } = new();
	public Guid Id { get; init; } = Guid.Empty;
}
