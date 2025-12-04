namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book.Metadata;

public class VolumeResponseDto {
	public Guid SeriesId { get; init; } = Guid.Empty;
	public int Order { get; init; }
	public string Title { get; init; } = string.Empty;
	public VolumeMetadata? Metadata { get; init; }
	public Guid Id { get; init; } = Guid.Empty;
}
