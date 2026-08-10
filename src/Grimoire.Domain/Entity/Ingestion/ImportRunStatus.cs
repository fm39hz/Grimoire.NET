namespace Grimoire.Domain.Entity.Ingestion;

public enum ImportRunStatus {
	Received,
	Analyzed,
	AwaitingDecision,
	ReadyToCommit,
	Committed,
	Failed
}
