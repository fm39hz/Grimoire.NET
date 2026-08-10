namespace Grimoire.Application.Ingestion.Analysis;

using Contract;
using Domain.Common;
using Domain.Common.Repository;
using Reconciliation;

public sealed class SeriesTargetResolver(
	IImportSourceRepository sourceRepository,
	ISeriesRepository seriesRepository) : ISeriesTargetResolver {
	public async Task<SeriesTargetResolution> Resolve(
		string producerId,
		TargetHintDto? hint,
		CancellationToken cancellationToken = default) {
		if (hint is null) return new SeriesTargetResolution(null, "No target hints were supplied.", []);
		if (!string.IsNullOrWhiteSpace(hint.SeriesId)) {
			var id = PrefixedId.ToGuid(hint.SeriesId, EntityPrefix.Series);
			return new SeriesTargetResolution(id, "The producer supplied an explicit series ID.", [hint.SeriesId]);
		}
		if (!string.IsNullOrWhiteSpace(hint.ProducerSeriesKey)) {
			var source = await sourceRepository.FindByIdentity(producerId, hint.ProducerSeriesKey, cancellationToken);
			if (source?.TargetSeriesId is not null) {
				return new SeriesTargetResolution(source.TargetSeriesId, "A producer series key has an existing source binding.",
					[PrefixedId.ToString(EntityPrefix.Series, source.TargetSeriesId.Value)]);
			}
		}

		var normalizedTitles = (hint.Titles ?? []).Select(TitleNormalizer.Normalize)
			.Where(static title => title.Length > 0).ToHashSet(StringComparer.Ordinal);
		if (normalizedTitles.Count == 0) return new SeriesTargetResolution(null, "No usable title hint was supplied.", []);
		var normalizedAuthors = (hint.Authors ?? []).Select(TitleNormalizer.Normalize)
			.Where(static author => author.Length > 0).ToHashSet(StringComparer.Ordinal);
		var page = await seriesRepository.FindAll(1, 1000, cancellationToken);
		var matches = page.Items.Where(series => normalizedTitles.Contains(TitleNormalizer.Normalize(series.Title)))
			.Select(series => new {
				series.Id,
				AuthorMatch = normalizedAuthors.Count == 0 || series.Metadata.Authors
					.Select(TitleNormalizer.Normalize).Any(normalizedAuthors.Contains)
			})
			.OrderByDescending(static match => match.AuthorMatch)
			.ToList();
		if (matches.Count == 1 && matches[0].AuthorMatch) {
			return new SeriesTargetResolution(matches[0].Id, "A unique normalized title/author match exists locally.",
				[PrefixedId.ToString(EntityPrefix.Series, matches[0].Id)]);
		}
		return new SeriesTargetResolution(null,
			matches.Count == 0 ? "No local series matched the supplied aliases." : "Multiple local series matched the supplied aliases.",
			matches.Select(match => PrefixedId.ToString(EntityPrefix.Series, match.Id)).ToList());
	}
}
