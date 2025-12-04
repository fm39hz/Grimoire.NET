namespace Grimoire.Application.Dto.Book.Segment;

public sealed record ImageSegmentDto : SegmentDto {
	public required string AssetKey { get; init; }
	public string? Caption { get; init; }
}
