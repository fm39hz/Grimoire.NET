namespace Grimoire.Application.Dto.Book;

public class ChapterListResponseDto {
	public Guid VolumeId { get; init; } = Guid.Empty;
	public int Order { get; init; }
	public string Title { get; init; } = string.Empty;
	public Guid Id { get; init; } = Guid.Empty;
}
