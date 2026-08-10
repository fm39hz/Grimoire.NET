namespace Grimoire.Application.Ingestion.Research;

public sealed record ResearchQuery(
	IReadOnlyList<string> Titles,
	IReadOnlyList<string> Authors,
	IReadOnlyList<string>? Isbns = null,
	IReadOnlyList<string>? SourceUris = null);

public sealed record ResearchCandidateDto(
	string Provider,
	string ExternalKey,
	string Kind,
	string Title,
	IReadOnlyList<string> Authors,
	IReadOnlyList<string> Isbns,
	int? FirstPublishedYear,
	string? Uri,
	double Confidence,
	IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record ResearchEvidenceDto(
	string Field,
	string Value,
	string Source,
	string? Uri,
	double Confidence,
	bool Confirmed = false,
	DateTimeOffset? ObservedAt = null);

public sealed record DiscoveredSourceDto(
	string Uri,
	string Provider,
	string? Relation,
	string? Title,
	double Confidence);

public sealed record ResearchProviderResult(
	string Provider,
	IReadOnlyList<ResearchCandidateDto> Candidates,
	IReadOnlyList<ResearchEvidenceDto> Evidence,
	IReadOnlyList<DiscoveredSourceDto> DiscoveredSources,
	string? Error = null);

public sealed record SeriesResearchProfileDto(
	string SeriesId,
	long Revision,
	IReadOnlyList<string> Aliases,
	IReadOnlyList<string> Creators,
	IReadOnlyList<ResearchEvidenceDto> Evidence,
	IReadOnlyList<ResearchCandidateDto> Candidates,
	IReadOnlyList<DiscoveredSourceDto> DiscoveredSources,
	IReadOnlyList<string> ProviderErrors,
	DateTimeOffset UpdatedAt);

public sealed record ConfirmResearchEvidenceDto(IReadOnlyList<ResearchEvidenceDto> Evidence);
