namespace Grimoire.Application.Ingestion.Execution;

using Contract;

public interface IImportExecutionService {
	Task<ImportRunResponseDto> SaveDecisions(Guid importRunId, UpdateImportDecisionsDto request, CancellationToken cancellationToken = default);
	Task<ImportRunResponseDto> Commit(Guid importRunId, bool safeOnly, CancellationToken cancellationToken = default);
}
