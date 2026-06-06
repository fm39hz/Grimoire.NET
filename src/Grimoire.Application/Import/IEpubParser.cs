namespace Grimoire.Application.Import;

public interface IEpubParser {
    Task<EpubParseResult> ParseAsync(Stream epubStream, CancellationToken cancellationToken = default);
}
