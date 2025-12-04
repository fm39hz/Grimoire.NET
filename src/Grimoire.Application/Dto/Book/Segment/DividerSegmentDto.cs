namespace Grimoire.Application.Dto.Book.Segment;

public sealed record DividerSegmentDto : SegmentDto {
	public string Style { get; init; } = "* * *";
}
