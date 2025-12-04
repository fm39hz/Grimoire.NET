namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book.Metadata;

public record CreateVolumeRequestDto(
	Guid SeriesId,
	int Order,
	string Title,
	VolumeMetadata? Metadata);
