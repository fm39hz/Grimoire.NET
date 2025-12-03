namespace Grimoire.Domain.Entity.Book.Segment;

public record FootnoteSegmentModel : SegmentModel {
	public List<TextSegmentModel> Segments { get; init; } = [];
}
