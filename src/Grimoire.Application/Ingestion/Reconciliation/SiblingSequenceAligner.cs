namespace Grimoire.Application.Ingestion.Reconciliation;

/// <summary>
///     Globally aligns sibling sequences. Low-confidence pairs are gaps even when their orders coincide.
/// </summary>
public sealed class SiblingSequenceAligner(NodeMatchScorer scorer) {
	private const double GapPenalty = -35;

	public SequenceAlignmentResult Align(
		IReadOnlyList<IncomingReconciliationNode> incoming,
		IReadOnlyList<ExistingReconciliationNode> existing) {
		var scores = new double[incoming.Count + 1, existing.Count + 1];
		var moves = new Move[incoming.Count + 1, existing.Count + 1];
		var pairScores = new (double Score, IReadOnlyList<MatchEvidence> Evidence)[incoming.Count, existing.Count];

		for (var i = 1; i <= incoming.Count; i++) {
			scores[i, 0] = scores[i - 1, 0] + GapPenalty;
			moves[i, 0] = Move.IncomingGap;
		}
		for (var j = 1; j <= existing.Count; j++) {
			scores[0, j] = scores[0, j - 1] + GapPenalty;
			moves[0, j] = Move.ExistingGap;
		}

		for (var i = 1; i <= incoming.Count; i++) {
			for (var j = 1; j <= existing.Count; j++) {
				var candidate = scorer.Score(incoming[i - 1], existing[j - 1]);
				pairScores[i - 1, j - 1] = candidate;
				var diagonal = candidate.Score >= NodeMatchScorer.MinimumMatchScore
					? scores[i - 1, j - 1] + candidate.Score
					: double.NegativeInfinity;
				var skipIncoming = scores[i - 1, j] + GapPenalty;
				var skipExisting = scores[i, j - 1] + GapPenalty;

				if (diagonal >= skipIncoming && diagonal >= skipExisting) {
					scores[i, j] = diagonal;
					moves[i, j] = Move.Match;
				}
				else if (skipIncoming >= skipExisting) {
					scores[i, j] = skipIncoming;
					moves[i, j] = Move.IncomingGap;
				}
				else {
					scores[i, j] = skipExisting;
					moves[i, j] = Move.ExistingGap;
				}
			}
		}

		var matches = new List<AlignmentMatch>();
		var incomingOnly = new List<int>();
		var existingOnly = new List<int>();
		var row = incoming.Count;
		var column = existing.Count;
		while (row > 0 || column > 0) {
			switch (moves[row, column]) {
				case Move.Match: {
					var pair = pairScores[row - 1, column - 1];
					matches.Add(new AlignmentMatch(row - 1, column - 1, pair.Score, pair.Evidence));
					row--;
					column--;
					break;
				}
				case Move.IncomingGap:
					incomingOnly.Add(row - 1);
					row--;
					break;
				case Move.ExistingGap:
					existingOnly.Add(column - 1);
					column--;
					break;
				default:
					throw new InvalidOperationException("Alignment traceback reached an invalid cell.");
			}
		}

		matches.Reverse();
		incomingOnly.Reverse();
		existingOnly.Reverse();
		return new SequenceAlignmentResult(matches, incomingOnly, existingOnly);
	}

	private enum Move {
		None,
		Match,
		IncomingGap,
		ExistingGap
	}
}
