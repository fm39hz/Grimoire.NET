namespace Grimoire.Application.Import;

public interface IImportStrategy {
	public string Format { get; }
	public bool CanHandle(string fileName);
	public Task<NormalizedImport> ParseAsync(Stream source, CancellationToken cancellationToken = default);
}
