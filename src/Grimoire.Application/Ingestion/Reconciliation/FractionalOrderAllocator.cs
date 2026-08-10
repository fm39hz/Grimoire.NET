namespace Grimoire.Application.Ingestion.Reconciliation;

public static class FractionalOrderAllocator {
	public static double Allocate(
		IEnumerable<(string Id, double Order)> siblingValues,
		double? hint,
		string? previousTargetId,
		string? nextTargetId) {
		var siblings = siblingValues.OrderBy(static sibling => sibling.Order).ToList();
		var previous = siblings.FirstOrDefault(sibling => sibling.Id == previousTargetId);
		var next = siblings.FirstOrDefault(sibling => sibling.Id == nextTargetId);
		if (previous.Id is not null && next.Id is not null) return previous.Order + (next.Order - previous.Order) / 2;
		if (previous.Id is not null) return previous.Order + 1;
		if (next.Id is not null) return next.Order - 1;
		if (hint is not null && siblings.All(sibling => sibling.Order != hint.Value)) return hint.Value;
		return siblings.Count == 0 ? hint ?? 1 : siblings[^1].Order + 1;
	}
}
