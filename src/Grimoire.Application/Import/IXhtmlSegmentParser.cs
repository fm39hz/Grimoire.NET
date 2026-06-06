namespace Grimoire.Application.Import;

public interface IXhtmlSegmentParser {
    ParsedChapter Parse(string html, IReadOnlyDictionary<string, byte[]> images);
}
