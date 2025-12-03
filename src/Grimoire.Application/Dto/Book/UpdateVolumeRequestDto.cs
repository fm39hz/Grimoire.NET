namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book.Metadata;

public record UpdateVolumeRequestDto(int? Order, string? Title, VolumeMetadata? Metadata);
