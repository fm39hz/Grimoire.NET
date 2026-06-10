namespace Grimoire.Application.Dto.Book;

using Metadata;

public record UpdateVolumeRequestDto(float? Order, string? Title, VolumeMetadataDto? Metadata, string? SeriesId = null);
