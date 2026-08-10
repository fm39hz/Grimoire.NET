namespace Grimoire.Application.Ingestion.Analysis;

using Contract;

public interface IImportAnalysisService {
	Task<ImportRunResponseDto> Analyze(SourcePackageDto package, CancellationToken cancellationToken = default);
	Task<ImportRunResponseDto?> Find(Guid importRunId, CancellationToken cancellationToken = default);
	Task RecordLegacyOutcome(Guid importRunId, string outcomeJson, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ImportRunResponseDto>> List(int limit = 50, CancellationToken cancellationToken = default);
}
