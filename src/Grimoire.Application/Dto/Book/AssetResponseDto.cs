namespace Grimoire.Application.Dto.Book;

public record AssetResponseDto {
	public required string Id { get; init; }
	public required string SeriesId { get; init; }
	public required string Path { get; init; }
	public required string FileHash { get; init; }
	public required string RefType { get; init; }
	public DateTime? CreatedAt { get; init; }
	public DateTime? UpdatedAt { get; init; }
}
