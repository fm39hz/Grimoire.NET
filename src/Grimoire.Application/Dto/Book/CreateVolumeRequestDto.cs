namespace Grimoire.Application.Dto.Book;

using Metadata;

public record CreateVolumeRequestDto(
	string SeriesId,
	double Order,
	string Title,
	VolumeMetadataDto? Metadata);
