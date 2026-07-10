namespace Grimoire.Application.Import;

public interface IXhtmlSegmentParser {
	public ParsedChapter Parse(string html, IReadOnlyDictionary<string, byte[]> images);
}
