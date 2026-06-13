namespace Grimoire.Application.Dto.Book;

using Metadata;

public record UpdateVolumeRequestDto(double? Order, string? Title, VolumeMetadataDto? Metadata, string? SeriesId = null);
