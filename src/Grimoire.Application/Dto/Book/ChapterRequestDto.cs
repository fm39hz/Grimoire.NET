namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book;

using Grimoire.Domain.Entity.Book.Segment;

public record ChapterRequestDto(
    Guid VolumeId,
    int Order,
    string Title,
    List<SegmentModel> Content,
    List<FootnoteSegmentModel> Footnotes) : IRequestDto<ChapterModel> {
    public ChapterModel ToModel() => new() {
        VolumeId = VolumeId,
        Order = Order,
        Title = Title,
        Content = Content,
        Footnotes = Footnotes
    };
}
