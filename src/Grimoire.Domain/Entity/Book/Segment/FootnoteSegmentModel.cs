namespace Grimoire.Domain.Entity.Book.Segment;

public class FootnoteSegmentModel : SegmentModel {
	public List<TextSegmentModel> Segments { get; set; } = [];
}
