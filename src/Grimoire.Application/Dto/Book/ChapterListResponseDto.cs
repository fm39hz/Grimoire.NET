namespace Grimoire.Application.Dto.Book;

public class ChapterListResponseDto {
	public string VolumeId { get; init; } = string.Empty;
	public double Order { get; init; }
	public string Title { get; init; } = string.Empty;
	public string Id { get; init; } = string.Empty;
	public DateTime? UpdatedAt { get; init; }
}
