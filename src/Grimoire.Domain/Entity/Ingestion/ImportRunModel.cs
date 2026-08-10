namespace Grimoire.Domain.Entity.Ingestion;

public sealed class ImportRunModel : BaseModel {
	public required string ProducerId { get; init; }
	public required string IdempotencyKey { get; init; }
	public Guid? TargetSeriesId { get; set; }
	public Guid ImportSourceId { get; init; }
	public ImportRunStatus Status { get; set; } = ImportRunStatus.Received;
	public ImportSemantics Semantics { get; init; }
	public required string PackageJson { get; init; }
	public required string PackageHash { get; init; }
	public long? BaseSeriesRevision { get; set; }
	public string? AnalysisJson { get; set; }
	public string? PlanJson { get; set; }
	public string? DecisionsJson { get; set; }
	public string? LegacyOutcomeJson { get; set; }
	public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
	public DateTimeOffset? AnalyzedAt { get; set; }
	public DateTimeOffset? CommittedAt { get; set; }
	public string? ErrorMessage { get; set; }
}
