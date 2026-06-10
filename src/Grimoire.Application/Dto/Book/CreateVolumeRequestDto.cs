namespace Grimoire.Application.Dto.Book;

using Metadata;

public record CreateVolumeRequestDto(
	string SeriesId,
	float Order,
	string Title,
	VolumeMetadataDto? Metadata);
