namespace Grimoire.Application.Ingestion.Reconciliation;

public sealed record IncomingReconciliationNode(
	string ExternalKey,
	ReconciliationNodeKind Kind,
	string Title,
	string? TargetId = null,
	string? LogicalKey = null,
	string? RoleHint = null,
	double? OrderHint = null,
	string? ContentHash = null);

public sealed record ExistingReconciliationNode(
	string TargetId,
	ReconciliationNodeKind Kind,
	string Title,
	string? LogicalKey = null,
	string? Role = null,
	double? Order = null,
	string? ContentHash = null,
	IReadOnlySet<string>? BoundExternalKeys = null);

public sealed record MatchEvidence(string Rule, double Score, string Description);

public sealed record AlignmentMatch(
	int IncomingIndex,
	int ExistingIndex,
	double Score,
	IReadOnlyList<MatchEvidence> Evidence);

public sealed record SequenceAlignmentResult(
	IReadOnlyList<AlignmentMatch> Matches,
	IReadOnlyList<int> IncomingOnly,
	IReadOnlyList<int> ExistingOnly);
