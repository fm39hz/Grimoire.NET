namespace Grimoire.Infrastructure.Export.Epub;

using Domain.Entity.Book;
using Domain.Entity.Book.Segment;

/// <summary>
///     Base view model for EPUB page rendering
/// </summary>
public abstract class PageViewModel {
	public required string Title { get; init; }
	public string? CustomCss { get; init; }
}

/// <summary>
///     View model for intro/title page rendering
/// </summary>
public class IntroPageViewModel : PageViewModel {
	public string? Author { get; init; }
	public string? CoverLocalPath { get; init; }
	public List<string>? Tags { get; init; }
	public List<TextSegmentModel>? Description { get; init; }
}

/// <summary>
///     View model for volume page rendering
/// </summary>
public class VolumePageViewModel : PageViewModel {
	public required string FileName { get; init; }
	public string? CoverImagePath { get; init; }
	public DateTime? PublicationDate { get; init; }
	public string? Isbn { get; init; }
}

/// <summary>
///     View model for chapter rendering in EPUB
/// </summary>
public class ChapterPageViewModel : PageViewModel {
	public required List<SegmentModel> Segments { get; init; }
	public List<FootnoteSegmentModel>? Footnotes { get; init; }
	public required string FileName { get; init; }
	public Dictionary<string, string>? ImageFileMap { get; init; }
}
