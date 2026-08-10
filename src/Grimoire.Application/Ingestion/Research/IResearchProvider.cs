namespace Grimoire.Application.Ingestion.Research;

public interface IResearchProvider {
	string Name { get; }
	Task<ResearchProviderResult> Search(ResearchQuery query, CancellationToken cancellationToken = default);
}
