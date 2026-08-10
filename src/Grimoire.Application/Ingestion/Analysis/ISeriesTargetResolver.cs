namespace Grimoire.Application.Ingestion.Analysis;

using Contract;

public sealed record SeriesTargetResolution(Guid? SeriesId, string Reason, IReadOnlyList<string> CandidateSeriesIds);

public interface ISeriesTargetResolver {
	Task<SeriesTargetResolution> Resolve(string producerId, TargetHintDto? hint, CancellationToken cancellationToken = default);
}
