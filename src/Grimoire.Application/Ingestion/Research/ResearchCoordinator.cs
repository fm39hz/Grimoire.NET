namespace Grimoire.Application.Ingestion.Research;

using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Ingestion;
using Reconciliation;
using Contract;

public sealed class ResearchCoordinator(
	IEnumerable<IResearchProvider> providers,
	ISeriesRepository seriesRepository,
	ISeriesResearchProfileRepository profileRepository) : IResearchCoordinator {
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
	};

	public async Task<SeriesResearchProfileDto> Research(
		Guid seriesId,
		bool forceRefresh = false,
		CancellationToken cancellationToken = default) {
		var existingModel = await profileRepository.FindBySeries(seriesId, cancellationToken);
		if (!forceRefresh && existingModel is not null) return Deserialize(existingModel);

		var series = await seriesRepository.FindOne(seriesId, cancellationToken)
			?? throw new KeyNotFoundException($"Series '{seriesId}' does not exist.");
		var existing = existingModel is null ? null : Deserialize(existingModel);
		var query = new ResearchQuery([series.Title, .. existing?.Aliases ?? []], [.. series.Metadata.Authors]);
		var tasks = providers.Select(provider => RunProvider(provider, query, cancellationToken)).ToArray();
		var results = await Task.WhenAll(tasks);

		var confirmedEvidence = existing?.Evidence.Where(static evidence => evidence.Confirmed).ToList() ?? [];
		var evidence = confirmedEvidence
			.Concat(results.SelectMany(static result => result.Evidence)
				.Where(candidate => confirmedEvidence.All(confirmed =>
					confirmed.Field != candidate.Field || confirmed.Value != candidate.Value)))
			.DistinctBy(static item => (item.Field, item.Value, item.Source))
			.ToList();
		var candidates = results.SelectMany(static result => result.Candidates)
			.GroupBy(static candidate => (TitleNormalizer.Normalize(candidate.Title),
				Author: TitleNormalizer.Normalize(candidate.Authors.FirstOrDefault())))
			.Select(static group => group.OrderByDescending(candidate => candidate.Confidence).First())
			.OrderByDescending(static candidate => candidate.Confidence)
			.ToList();
		var discovered = (existing?.DiscoveredSources ?? [])
			.Concat(results.SelectMany(static result => result.DiscoveredSources))
			.GroupBy(static source => source.Uri, StringComparer.OrdinalIgnoreCase)
			.Select(static group => group.OrderByDescending(source => source.Confidence).First())
			.ToList();
		var aliases = evidence.Where(static item => item.Field == "title")
			.Select(static item => item.Value).Append(series.Title).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		var creators = evidence.Where(static item => item.Field == "author")
			.Select(static item => item.Value).Concat(series.Metadata.Authors).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		var profile = new SeriesResearchProfileDto(
			PrefixedId.ToString(EntityPrefix.Series, seriesId),
			(existing?.Revision ?? 0) + 1,
			aliases,
			creators,
			evidence,
			candidates,
			discovered,
			results.Where(static result => result.Error is not null).Select(result => $"{result.Provider}: {result.Error}").ToList(),
			DateTimeOffset.UtcNow);

		await Save(seriesId, existingModel, profile, cancellationToken);
		return profile;
	}

	public async Task<SeriesResearchProfileDto?> Find(Guid seriesId, CancellationToken cancellationToken = default) {
		var model = await profileRepository.FindBySeries(seriesId, cancellationToken);
		return model is null ? null : Deserialize(model);
	}

	public async Task<SeriesResearchProfileDto> Confirm(
		Guid seriesId,
		ConfirmResearchEvidenceDto request,
		CancellationToken cancellationToken = default) {
		var model = await profileRepository.FindBySeries(seriesId, cancellationToken);
		var current = model is null
			? new SeriesResearchProfileDto(PrefixedId.ToString(EntityPrefix.Series, seriesId), 0, [], [], [], [], [], [], DateTimeOffset.UtcNow)
			: Deserialize(model);
		var confirmed = request.Evidence.Select(static evidence => evidence with {
			Confirmed = true,
			Source = string.IsNullOrWhiteSpace(evidence.Source) ? "user" : evidence.Source,
			Confidence = 1,
			ObservedAt = evidence.ObservedAt ?? DateTimeOffset.UtcNow
		}).ToList();
		var merged = current.Evidence
			.Where(item => confirmed.All(replacement => replacement.Field != item.Field || replacement.Value != item.Value))
			.Concat(confirmed).ToList();
		var updated = current with {
			Revision = current.Revision + 1,
			Evidence = merged,
			Aliases = merged.Where(static item => item.Field == "title").Select(static item => item.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			Creators = merged.Where(static item => item.Field == "author").Select(static item => item.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			UpdatedAt = DateTimeOffset.UtcNow
		};
		await Save(seriesId, model, updated, cancellationToken);
		return updated;
	}

	public async Task ObserveSourcePackage(Guid seriesId, SourcePackageDto package, CancellationToken cancellationToken = default) {
		var model = await profileRepository.FindBySeries(seriesId, cancellationToken);
		var current = model is null
			? new SeriesResearchProfileDto(PrefixedId.ToString(EntityPrefix.Series, seriesId), 0,
				package.TargetHint?.Titles ?? [], package.TargetHint?.Authors ?? [], [], [], [], [], DateTimeOffset.UtcNow)
			: Deserialize(model);
		var nodes = Flatten(package.Nodes).ToList();
		var observedEvidence = new List<ResearchEvidenceDto>();
		foreach (var title in package.TargetHint?.Titles ?? []) {
			observedEvidence.Add(new ResearchEvidenceDto("title", title, package.Source.Provider, package.Source.Uri, 0.7,
				ObservedAt: package.Source.ObservedAt ?? DateTimeOffset.UtcNow));
		}
		foreach (var author in package.TargetHint?.Authors ?? []) {
			observedEvidence.Add(new ResearchEvidenceDto("author", author, package.Source.Provider, package.Source.Uri, 0.7,
				ObservedAt: package.Source.ObservedAt ?? DateTimeOffset.UtcNow));
		}
		foreach (var node in nodes.Where(static node => node.RoleHint?.Contains("placeholder", StringComparison.OrdinalIgnoreCase) == true)) {
			observedEvidence.Add(new ResearchEvidenceDto("coverage-gap", node.Title, package.Source.Provider, package.Source.Uri, 0.9,
				ObservedAt: package.Source.ObservedAt ?? DateTimeOffset.UtcNow));
		}
		var discovered = nodes.SelectMany(static node => node.Relations ?? [])
			.Where(static relation => !string.IsNullOrWhiteSpace(relation.Uri))
			.Select(relation => new DiscoveredSourceDto(relation.Uri!, package.Source.Provider, relation.Type, null, 0.9))
			.Append(package.Source.Uri is null
				? null
				: new DiscoveredSourceDto(package.Source.Uri, package.Source.Provider, "observed-source", package.TargetHint?.Titles?.FirstOrDefault(), 1))
			.Where(static source => source is not null).Cast<DiscoveredSourceDto>();
		var mergedEvidence = current.Evidence.Concat(observedEvidence)
			.GroupBy(static evidence => (evidence.Field, evidence.Value, evidence.Source))
			.Select(static group => group.OrderByDescending(evidence => evidence.Confirmed).ThenByDescending(evidence => evidence.Confidence).First())
			.ToList();
		var updated = current with {
			Revision = current.Revision + 1,
			Aliases = current.Aliases.Concat(package.TargetHint?.Titles ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			Creators = current.Creators.Concat(package.TargetHint?.Authors ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			Evidence = mergedEvidence,
			DiscoveredSources = current.DiscoveredSources.Concat(discovered)
				.GroupBy(static source => source.Uri, StringComparer.OrdinalIgnoreCase)
				.Select(static group => group.OrderByDescending(source => source.Confidence).First()).ToList(),
			UpdatedAt = DateTimeOffset.UtcNow
		};
		await Save(seriesId, model, updated, cancellationToken);
	}

	private static IEnumerable<SourceNodeDto> Flatten(IEnumerable<SourceNodeDto> nodes) {
		foreach (var node in nodes) {
			yield return node;
			foreach (var child in Flatten(node.Children ?? [])) yield return child;
		}
	}

	private static async Task<ResearchProviderResult> RunProvider(
		IResearchProvider provider,
		ResearchQuery query,
		CancellationToken cancellationToken) {
		try {
			return await provider.Search(query, cancellationToken);
		}
		catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested) {
			return new ResearchProviderResult(provider.Name, [], [], [], exception.Message);
		}
	}

	private async Task Save(
		Guid seriesId,
		SeriesResearchProfileModel? model,
		SeriesResearchProfileDto profile,
		CancellationToken cancellationToken) {
		var json = JsonSerializer.Serialize(profile, JsonOptions);
		if (model is null) {
			await profileRepository.Create(new SeriesResearchProfileModel {
				SeriesId = seriesId,
				Revision = profile.Revision,
				ProfileJson = json
			}, cancellationToken);
		}
		else {
			model.Revision = profile.Revision;
			model.ProfileJson = json;
			await profileRepository.Update(model, cancellationToken);
		}
	}

	private static SeriesResearchProfileDto Deserialize(SeriesResearchProfileModel model) =>
		JsonSerializer.Deserialize<SeriesResearchProfileDto>(model.ProfileJson, JsonOptions)
		?? throw new InvalidOperationException($"Research profile '{model.Id}' is invalid.");
}
