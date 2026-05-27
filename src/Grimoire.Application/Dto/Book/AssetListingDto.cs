namespace Grimoire.Application.Dto.Book;

public record AssetListingDto {
	public required string Id { get; init; }
	public required string RefType { get; init; }
	public required string FileName { get; init; }
}
