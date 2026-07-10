namespace Grimoire.Application.Import;

public interface IEpubParser {
	public Task<EpubParseResult> ParseAsync(Stream epubStream, CancellationToken cancellationToken = default);
}
