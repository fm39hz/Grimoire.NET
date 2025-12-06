namespace Grimoire.Application.Dto.Book;

using Metadata;

public record CreateVolumeRequestDto(
	string SeriesId,
	int Order,
	string Title,
	VolumeMetadataDto? Metadata);
