namespace Grimoire.Application.Dto.Book.Tree;

public enum BookTreeNodeType {
	BookShelf,
	Series,
	Volume,
	Chapter
}

public sealed record BookTreeDto(BookTreeNodeDto Root);

public sealed record BookTreeNodeDto {
	public required string Id { get; init; }
	public required BookTreeNodeType Type { get; init; }
	public required string Title { get; init; }
	public double? Order { get; init; }
	public string? ParentId { get; init; }
	public List<BookTreeNodeDto> Children { get; init; } = [];
}
