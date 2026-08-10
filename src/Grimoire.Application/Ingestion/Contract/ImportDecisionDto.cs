namespace Grimoire.Application.Ingestion.Contract;

public enum ImportDecisionChoice {
	Approve,
	Reject
}

public sealed record ImportOperationDecisionDto(string OperationId, ImportDecisionChoice Choice, string? Note = null);

public sealed record UpdateImportDecisionsDto(IReadOnlyList<ImportOperationDecisionDto> Decisions);
