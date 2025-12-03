namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book.Segment;

public record ImportFootnoteDto {
	public string? InitialId { get; init; }
	public List<TextSegmentModel> Segments { get; init; } = [];
}
