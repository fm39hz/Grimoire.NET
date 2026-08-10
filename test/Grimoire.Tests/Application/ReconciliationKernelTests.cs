namespace Grimoire.Tests.Application;

using Grimoire.Application.Ingestion.Reconciliation;
using Xunit;

public sealed class ReconciliationKernelTests {
	private readonly SiblingSequenceAligner aligner = new(new NodeMatchScorer());

	[Fact]
	public void Align_DoesNotTreatEqualOrderAsIdentity() {
		var result = aligner.Align(
			[new IncomingReconciliationNode("source:one", ReconciliationNodeKind.Content, "Completely new chapter", OrderHint: 1)],
			[new ExistingReconciliationNode("chap_existing", ReconciliationNodeKind.Content, "Unrelated old chapter", Order: 1)]);

		Assert.Empty(result.Matches);
		Assert.Equal([0], result.IncomingOnly);
		Assert.Equal([0], result.ExistingOnly);
	}

	[Fact]
	public void Align_UsesGlobalSequenceWhenAPrologueWasInserted() {
		var incoming = new[] {
			new IncomingReconciliationNode("prologue", ReconciliationNodeKind.Content, "Prologue", OrderHint: 1),
			new IncomingReconciliationNode("one", ReconciliationNodeKind.Content, "Chapter 1", OrderHint: 2),
			new IncomingReconciliationNode("two", ReconciliationNodeKind.Content, "Chapter 2", OrderHint: 3)
		};
		var existing = new[] {
			new ExistingReconciliationNode("chap_1", ReconciliationNodeKind.Content, "Chapter 1", Order: 1),
			new ExistingReconciliationNode("chap_2", ReconciliationNodeKind.Content, "Chapter 2", Order: 2)
		};

		var result = aligner.Align(incoming, existing);

		Assert.Equal([(1, 0), (2, 1)], result.Matches.Select(static match => (match.IncomingIndex, match.ExistingIndex)));
		Assert.Equal([0], result.IncomingOnly);
		Assert.Empty(result.ExistingOnly);
	}

	[Fact]
	public void Align_ExactBindingOutranksSimilarTitle() {
		var result = aligner.Align(
			[new IncomingReconciliationNode("provider:42", ReconciliationNodeKind.Content, "Renamed", OrderHint: 1)],
			[
				new ExistingReconciliationNode("chap_wrong", ReconciliationNodeKind.Content, "Renamed", Order: 1),
				new ExistingReconciliationNode("chap_bound", ReconciliationNodeKind.Content, "Old title", Order: 2,
					BoundExternalKeys: new HashSet<string> { "provider:42" })
			]);

		var match = Assert.Single(result.Matches);
		Assert.Equal(1, match.ExistingIndex);
		Assert.Contains(match.Evidence, static evidence => evidence.Rule == "binding");
	}

	[Fact]
	public void Align_DoesNotMergeDifferentExplicitOrdinalsFromFuzzyTitles() {
		var result = aligner.Align(
			[new IncomingReconciliationNode("arc-1", ReconciliationNodeKind.Container, "Arc 1", OrderHint: 1)],
			[new ExistingReconciliationNode("vol_arc_2", ReconciliationNodeKind.Container, "Arc 2", Order: 2)]);

		Assert.Empty(result.Matches);
		Assert.Equal([0], result.IncomingOnly);
		Assert.Equal([0], result.ExistingOnly);
	}

	[Theory]
	[InlineData("same", "same", "same", ContentReconciliationDecision.NoChange)]
	[InlineData("base", "base", "remote", ContentReconciliationDecision.ReplaceLocal)]
	[InlineData("base", "local", "base", ContentReconciliationDecision.KeepLocal)]
	[InlineData("base", "same-new", "same-new", ContentReconciliationDecision.Converged)]
	[InlineData("base", "local", "remote", ContentReconciliationDecision.Conflict)]
	[InlineData(null, null, "new", ContentReconciliationDecision.Create)]
	[InlineData(null, "local", "remote", ContentReconciliationDecision.Conflict)]
	public void ThreeWayComparison_IsExplicit(string? basis, string? local, string? incoming, ContentReconciliationDecision expected) {
		Assert.Equal(expected, ThreeWayContentComparer.Compare(basis, local, incoming));
	}

	[Fact]
	public void Policy_RequiresReviewForCompetingPrimarySources() {
		var decision = ReconciliationPolicy.Evaluate(new PolicyContext(
			ProposedChangeKind.UpdateBoundContent,
			ContentReconciliationDecision.ReplaceLocal,
			HasCompetingPrimary: true));

		Assert.Equal(PlanDisposition.Review, decision.Disposition);
		Assert.Equal("MultiplePrimarySources", decision.Code);
	}

	[Fact]
	public void Policy_AllowsOnlySafeBoundReplacementAutomatically() {
		var safe = ReconciliationPolicy.Evaluate(new PolicyContext(
			ProposedChangeKind.UpdateBoundContent,
			ContentReconciliationDecision.ReplaceLocal));
		var conflict = ReconciliationPolicy.Evaluate(new PolicyContext(
			ProposedChangeKind.UpdateBoundContent,
			ContentReconciliationDecision.Conflict));

		Assert.Equal(PlanDisposition.Automatic, safe.Disposition);
		Assert.Equal(PlanDisposition.Review, conflict.Disposition);
	}
}
