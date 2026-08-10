namespace Grimoire.Infrastructure.Research;

using Application.Ingestion.Reconciliation;
using Application.Ingestion.Research;
using Domain.Common;
using Domain.Common.Repository;

public sealed class LocalSeriesResearchProvider(ISeriesRepository repository) : IResearchProvider {
	public string Name => "local";

	public async Task<ResearchProviderResult> Search(ResearchQuery query, CancellationToken cancellationToken = default) {
		var page = await repository.FindAll(1, 1000, cancellationToken);
		var normalizedTitles = query.Titles.Select(TitleNormalizer.Normalize).Where(static title => title.Length > 0).ToHashSet();
		var matches = page.Items.Where(series => normalizedTitles.Contains(TitleNormalizer.Normalize(series.Title))).ToList();
		var candidates = matches.Select(series => new ResearchCandidateDto(
			Name,
			PrefixedId.ToString(EntityPrefix.Series, series.Id),
			"editorial-series",
			series.Title,
			[.. series.Metadata.Authors],
			[],
			null,
			null,
			1)).ToList();
		return new ResearchProviderResult(Name, candidates, [], []);
	}
}
