namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book;

public record ChapterRequestDto(
	Guid VolumeId,
	int Order,
	string Title,
	List<SegmentModel> Content
	) : IRequestDto<ChapterModel> {
	public ChapterModel ToModel() => new() { VolumeId = VolumeId, Order = Order, Title = Title, Content = Content };
}
