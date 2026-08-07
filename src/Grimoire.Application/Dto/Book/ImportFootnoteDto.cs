namespace Grimoire.Application.Dto.Book;

using Segment;

public record ImportFootnoteDto {
	public string? InitialId { get; init; }
	public List<TextSegmentDto> Segments { get; init; } = [];
}
