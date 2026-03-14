namespace Grimoire.Infrastructure.Export.Epub;

/// <summary>
///     Context information for segment rendering
/// </summary>
public class SegmentRenderContext {
	public Dictionary<string, string>? ImageFileMap { get; init; }
	public Dictionary<string, int>? FootnoteMap { get; init; }
}
