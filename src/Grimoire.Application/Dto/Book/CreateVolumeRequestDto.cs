namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book.Metadata;

public record CreateVolumeRequestDto(
	string SeriesId,
	int Order,
	string Title,
	VolumeMetadata? Metadata);
