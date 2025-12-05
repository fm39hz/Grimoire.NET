namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book.Metadata;

public class VolumeResponseDto {
	public string SeriesId { get; init; } = string.Empty;
	public int Order { get; init; }
	public string Title { get; init; } = string.Empty;
	public VolumeMetadata? Metadata { get; init; }
	public string Id { get; init; } = string.Empty;
}
