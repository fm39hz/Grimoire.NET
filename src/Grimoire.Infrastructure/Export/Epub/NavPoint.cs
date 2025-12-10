namespace Grimoire.Infrastructure.Export.Epub;

/// <summary>
///     Navigation point for EPUB table of contents
/// </summary>
public class NavPoint {
	public required string Title { get; init; }
	public required string ContentSrc { get; init; }
	public List<NavPoint>? Children { get; init; }
}
