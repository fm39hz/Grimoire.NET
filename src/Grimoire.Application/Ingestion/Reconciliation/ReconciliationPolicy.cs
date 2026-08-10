namespace Grimoire.Application.Ingestion.Reconciliation;

public enum PlanDisposition {
	Automatic,
	Review,
	Reject
}

public enum ProposedChangeKind {
	CreateNode,
	UpdateBoundContent,
	AttachSupplement,
	RefreshBinding,
	MoveAcrossParent,
	RemoveFromSnapshot,
	ReplacePinnedPrimary,
	ReplaceUnboundContent,
	MergeProse,
	InvalidStructure
}

public sealed record PolicyContext(
	ProposedChangeKind Change,
	ContentReconciliationDecision ContentDecision = ContentReconciliationDecision.NoChange,
	bool HasCompetingPrimary = false);

public sealed record PolicyDecision(PlanDisposition Disposition, string Code, string Explanation);

public static class ReconciliationPolicy {
	public static PolicyDecision Evaluate(PolicyContext context) {
		if (context.Change == ProposedChangeKind.InvalidStructure) {
			return new PolicyDecision(PlanDisposition.Reject, "InvalidStructure", "The package violates a domain invariant.");
		}
		if (context.HasCompetingPrimary) {
			return new PolicyDecision(PlanDisposition.Review, "MultiplePrimarySources", "Two sources claim primary ownership.");
		}
		if (context.ContentDecision == ContentReconciliationDecision.Conflict) {
			return new PolicyDecision(PlanDisposition.Review, "ConcurrentLocalAndIncomingEdit", "Local and incoming content diverged from their base.");
		}

		return context.Change switch {
			ProposedChangeKind.CreateNode => Automatic("CreateNode", "Creating a new unambiguous node is safe."),
			ProposedChangeKind.UpdateBoundContent when context.ContentDecision is ContentReconciliationDecision.ReplaceLocal or ContentReconciliationDecision.NoChange or ContentReconciliationDecision.Converged
				=> Automatic("BoundContent", "The exact binding and three-way comparison make the update safe."),
			ProposedChangeKind.AttachSupplement => Automatic("AttachSupplement", "Supplemental material cannot replace primary content."),
			ProposedChangeKind.RefreshBinding => Automatic("RefreshBinding", "Refreshing identity metadata does not replace editorial content."),
			ProposedChangeKind.MoveAcrossParent => Review("CrossParentMove", "Moving a node across parents changes book structure."),
			ProposedChangeKind.RemoveFromSnapshot => Review("SnapshotRemoval", "Snapshot omission proposes removal and requires review."),
			ProposedChangeKind.ReplacePinnedPrimary => Review("PinnedPrimary", "Pinned primary content cannot be replaced automatically."),
			ProposedChangeKind.ReplaceUnboundContent => Review("UnboundReplacement", "There is no exact binding for the replacement."),
			ProposedChangeKind.MergeProse => Review("ProseMerge", "Prose is never merged automatically."),
			_ => Review("PolicyRequired", "No automatic policy covers this change.")
		};
	}

	private static PolicyDecision Automatic(string code, string explanation) => new(PlanDisposition.Automatic, code, explanation);
	private static PolicyDecision Review(string code, string explanation) => new(PlanDisposition.Review, code, explanation);
}
