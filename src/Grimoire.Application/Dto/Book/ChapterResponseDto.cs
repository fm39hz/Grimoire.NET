namespace Grimoire.Application.Dto.Book;

using Common;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;

public class ChapterResponseDto(ChapterModel chapter) : IResponseDto {
	public Guid VolumeId { get; init; } = chapter.VolumeId;
	public int Order { get; init; } = chapter.Order;
	public string Title { get; init; } = chapter.Title;
	public List<SegmentModel> Content { get; init; } = chapter.Content;
	public List<FootnoteSegmentModel> Footnotes { get; init; } = chapter.Footnotes;
	public Guid Id { get; init; } = chapter.Id;
}
