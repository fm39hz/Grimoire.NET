namespace Grimoire.Application.Ingestion.Reconciliation;

using System.Text.RegularExpressions;

public sealed class NodeMatchScorer {
	public const double MinimumMatchScore = 60;

	public (double Score, IReadOnlyList<MatchEvidence> Evidence) Score(
		IncomingReconciliationNode incoming,
		ExistingReconciliationNode existing) {
		if (incoming.Kind != existing.Kind) {
			return (double.NegativeInfinity,
				[new MatchEvidence("type-conflict", double.NegativeInfinity, "Node kinds are incompatible.")]);
		}

		var evidence = new List<MatchEvidence>();
		if (!string.IsNullOrWhiteSpace(incoming.TargetId) && incoming.TargetId == existing.TargetId) {
			evidence.Add(new MatchEvidence("target-id", 1000, "The producer supplied the exact editorial target ID."));
		}
		if (existing.BoundExternalKeys?.Contains(incoming.ExternalKey) == true) {
			evidence.Add(new MatchEvidence("binding", 1000, "An existing import binding resolves this external key."));
		}
		if (!string.IsNullOrWhiteSpace(incoming.LogicalKey) && incoming.LogicalKey == existing.LogicalKey) {
			evidence.Add(new MatchEvidence("logical-key", 500, "Producer and editorial nodes share a logical key."));
		}

		var incomingTitle = TitleNormalizer.Normalize(incoming.Title);
		var existingTitle = TitleNormalizer.Normalize(existing.Title);
		var hasStrongIdentity = evidence.Any(static item => item.Score >= 500);
		var incomingOrdinals = Ordinals(incomingTitle);
		var existingOrdinals = Ordinals(existingTitle);
		if (!hasStrongIdentity && incomingOrdinals.Count > 0 && existingOrdinals.Count > 0 &&
			!incomingOrdinals.SequenceEqual(existingOrdinals, StringComparer.Ordinal)) {
			evidence.Add(new MatchEvidence("ordinal-conflict", -100,
				"Titles contain different explicit ordinals; fuzzy wording cannot merge them."));
		}
		if (incomingTitle.Length > 0 && incomingTitle == existingTitle) {
			evidence.Add(new MatchEvidence("exact-title", 100, "Normalized titles are equal."));
		}
		else {
			var similarity = Similarity(incomingTitle, existingTitle);
			if (similarity >= 0.6) {
				var fuzzyScore = Math.Round(similarity * 80, 3);
				evidence.Add(new MatchEvidence("fuzzy-title", fuzzyScore, $"Normalized title similarity is {similarity:P1}."));
			}
		}

		if (!string.IsNullOrWhiteSpace(incoming.RoleHint) &&
			string.Equals(TitleNormalizer.Normalize(incoming.RoleHint), TitleNormalizer.Normalize(existing.Role), StringComparison.Ordinal)) {
			evidence.Add(new MatchEvidence("role", 25, "Role hints are compatible."));
		}
		if (!string.IsNullOrWhiteSpace(incoming.ContentHash) && incoming.ContentHash == existing.ContentHash) {
			evidence.Add(new MatchEvidence("content-hash", 100, "Content hashes are equal."));
		}
		if (incoming.OrderHint is not null && existing.Order is not null) {
			var proximity = Math.Round(20 / (1 + Math.Abs(incoming.OrderHint.Value - existing.Order.Value)), 3);
			evidence.Add(new MatchEvidence("order-proximity", proximity, "Sibling order is a positional hint, not identity."));
		}

		return (evidence.Sum(static item => item.Score), evidence);
	}

	private static IReadOnlyList<string> Ordinals(string title) =>
		[.. Regex.Matches(title, @"\d+(?:\.\d+)?").Select(static match => match.Value.TrimStart('0'))];

	private static double Similarity(string left, string right) {
		if (left == right) return 1;
		if (left.Length == 0 || right.Length == 0) return 0;

		var previous = Enumerable.Range(0, right.Length + 1).ToArray();
		var current = new int[right.Length + 1];
		for (var i = 1; i <= left.Length; i++) {
			current[0] = i;
			for (var j = 1; j <= right.Length; j++) {
				var substitution = previous[j - 1] + (left[i - 1] == right[j - 1] ? 0 : 1);
				current[j] = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1), substitution);
			}
			(previous, current) = (current, previous);
		}

		return 1d - (double)previous[right.Length] / Math.Max(left.Length, right.Length);
	}
}
