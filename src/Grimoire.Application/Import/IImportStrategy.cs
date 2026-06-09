namespace Grimoire.Application.Import;

public interface IImportStrategy {
    string Format { get; }
    bool CanHandle(string fileName);
    Task<NormalizedImport> ParseAsync(Stream source, CancellationToken cancellationToken = default);
}
