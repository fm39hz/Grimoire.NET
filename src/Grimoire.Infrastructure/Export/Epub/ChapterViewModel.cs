namespace Grimoire.Infrastructure.Export.Epub;

using Domain.Entity.Book;
using Domain.Entity.Book.Segment;

/// <summary>
///     View model for chapter rendering in EPUB
/// </summary>
public class ChapterViewModel {
	public required string Title { get; init; }
	public required List<SegmentModel> Segments { get; init; }
	public List<FootnoteSegmentModel>? Footnotes { get; init; }
	public required string FileName { get; init; }
	public Dictionary<string, string>? ImageFileMap { get; init; }
}
