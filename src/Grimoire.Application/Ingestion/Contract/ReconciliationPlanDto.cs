namespace Grimoire.Application.Ingestion.Contract;

using Reconciliation;

public sealed record ReconciliationPlanDto(
	string ImportRunId,
	string? TargetSeriesId,
	long? BaseSeriesRevision,
	IReadOnlyList<PlanOperationDto> Operations,
	IReadOnlyList<PlanIssueDto> Issues,
	PlanSummaryDto Summary);

public sealed record PlanOperationDto(
	string Id,
	string Type,
	PlanDisposition Disposition,
	string ExternalNodeKey,
	string? TargetNodeId,
	string Description,
	IReadOnlyList<MatchEvidence>? Evidence = null,
	string? ParentTargetId = null,
	string? ParentOperationId = null,
	SourceNodeDto? SourceNode = null,
	string? PreviousTargetId = null,
	string? NextTargetId = null);

public sealed record PlanIssueDto(string Code, string Message, string? ExternalNodeKey = null);

public sealed record PlanSummaryDto(int Automatic, int Review, int Rejected, int NoOp);

public sealed record ImportRunResponseDto(
	string Id,
	string Status,
	string ProducerId,
	string IdempotencyKey,
	string? TargetSeriesId,
	long? BaseSeriesRevision,
	DateTimeOffset StartedAt,
	DateTimeOffset? AnalyzedAt,
	ReconciliationPlanDto? Plan);
