namespace Grimoire.Infrastructure.Export.Epub;

using Domain.Entity.Book.Segment;

/// <summary>
///     View model for intro/title page rendering
/// </summary>
public class IntroViewModel {
	public required string BookTitle { get; init; }
	public string? Author { get; init; }
	public string? CoverLocalPath { get; init; }
	public List<string>? Tags { get; init; }
	public List<TextSegmentModel>? Description { get; init; }
	public string? Epigraph { get; init; }
	public string? Foreword { get; init; }
}
