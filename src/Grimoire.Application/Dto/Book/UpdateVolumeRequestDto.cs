namespace Grimoire.Application.Dto.Book;

using Metadata;

public record UpdateVolumeRequestDto(int? Order, string? Title, VolumeMetadataDto? Metadata);
