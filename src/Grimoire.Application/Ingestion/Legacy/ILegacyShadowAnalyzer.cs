namespace Grimoire.Application.Ingestion.Legacy;

using Contract;

public interface ILegacyShadowAnalyzer {
	Task<Guid?> Analyze(SourcePackageDto package, CancellationToken cancellationToken = default);
	Task RecordOutcome(Guid? importRunId, object outcome, CancellationToken cancellationToken = default);
}
