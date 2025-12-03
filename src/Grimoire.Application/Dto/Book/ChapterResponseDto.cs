namespace Grimoire.Application.Dto.Book;

using Common;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;

public class ChapterResponseDto(ChapterModel chapter) : IResponseDto {
	public Guid VolumeId { get; init; } = chapter.VolumeId;
	public float Order { get; init; } = chapter.Order;
	public string Title { get; init; } = chapter.Title;
	public ICollection<ChapterVariantModel> Variants { get; init; } = chapter.Variants;
	public Guid Id { get; init; } = chapter.Id;
}
