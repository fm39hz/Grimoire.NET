namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book;
using Domain.Entity.Book.Metadata;

public record CreateVolumeRequestDto(
	Guid SeriesId,
	int Order,
	string Title,
	VolumeMetadata? Metadata) : IRequestDto<VolumeModel> {
	public VolumeModel ToModel() => new() { SeriesId = SeriesId, Order = Order, Title = Title, Metadata = Metadata };
}
