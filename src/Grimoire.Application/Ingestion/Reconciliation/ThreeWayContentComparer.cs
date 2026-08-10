namespace Grimoire.Application.Ingestion.Reconciliation;

public enum ContentReconciliationDecision {
	NoChange,
	Create,
	ReplaceLocal,
	KeepLocal,
	Converged,
	Conflict
}

public static class ThreeWayContentComparer {
	public static ContentReconciliationDecision Compare(string? baseHash, string? localHash, string? incomingHash) {
		if (incomingHash == localHash) return baseHash == incomingHash
			? ContentReconciliationDecision.NoChange
			: ContentReconciliationDecision.Converged;

		if (baseHash is null) {
			return localHash is null
				? ContentReconciliationDecision.Create
				: ContentReconciliationDecision.Conflict;
		}

		if (incomingHash == baseHash) return ContentReconciliationDecision.KeepLocal;
		if (localHash == baseHash) return ContentReconciliationDecision.ReplaceLocal;
		return ContentReconciliationDecision.Conflict;
	}
}
