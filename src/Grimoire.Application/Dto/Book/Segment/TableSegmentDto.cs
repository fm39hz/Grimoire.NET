namespace Grimoire.Application.Dto.Book.Segment;

public sealed record TableCellDto(IReadOnlyList<TextRunDto>? Runs);

public sealed record TableSegmentDto : SegmentDto {
	public List<TableCellDto> Header { get; init; } = [];
	public List<List<TableCellDto>> Rows { get; init; } = [];
}
