namespace Grimoire.Application.Dto.Book;

using Common;
using Domain.Entity.Book;
using Domain.Entity.Book.Metadata;

public class VolumeResponseDto(VolumeModel volume) : IResponseDto {
	public Guid SeriesId { get; init; } = volume.SeriesId;
	public int Order { get; init; } = volume.Order;
	public string Title { get; init; } = volume.Title;
	public VolumeMetadata? Metadata { get; init; } = volume.Metadata;
	public Guid Id { get; init; } = volume.Id;
}
