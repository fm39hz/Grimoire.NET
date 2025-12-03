namespace Grimoire.Domain.Entity.Book.Segment;

public record FootnoteSegmentModel : SegmentModel {
	public string InitialId { get; set; } = string.Empty;
	public List<TextSegmentModel> Segments { get; init; } = [];
}
