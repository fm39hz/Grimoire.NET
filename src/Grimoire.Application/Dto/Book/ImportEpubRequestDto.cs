namespace Grimoire.Application.Dto.Book;

/// <summary>
///     Options for importing EPUB files into the system.
///     All series metadata (title, author, cover, etc.) is extracted from the EPUB file itself.
/// </summary>
public record ImportEpubRequestDto {
	/// <summary>
	///     Existing series ID to add volumes/chapters to.
	///     If not provided, a new series will be created automatically.
	/// </summary>
	public string? ExistingSeriesId { get; init; }
}
