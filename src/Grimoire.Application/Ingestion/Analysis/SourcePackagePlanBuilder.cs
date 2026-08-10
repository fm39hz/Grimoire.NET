namespace Grimoire.Application.Ingestion.Analysis;

using Contract;
using Domain.Entity.Ingestion;
using Dto.Book.Tree;
using Reconciliation;

public sealed class SourcePackagePlanBuilder(SiblingSequenceAligner aligner) {
	public ReconciliationPlanDto Build(
		Guid runId,
		SourcePackageDto package,
		string? targetSeriesId,
		long? baseRevision,
		BookTreeDto? tree,
		IReadOnlyList<ImportBindingModel> bindings,
		Guid? currentSourceId = null,
		IReadOnlyList<ImportBindingModel>? allBindings = null) {
		var operations = new List<PlanOperationDto>();
		var issues = new List<PlanIssueDto>();
		if (tree is null || targetSeriesId is null) {
			issues.Add(new PlanIssueDto("TargetRequired", "The package could not be resolved to one editorial series."));
			return CreatePlan(runId, targetSeriesId, baseRevision, operations, issues);
		}

		var seriesNode = tree.Root.Children.Single(static node => node.Type == BookTreeNodeType.Series);
		var sourceRoots = package.Nodes.Count == 1 && LooksLikeSeries(package.Nodes[0])
			? package.Nodes[0].Children ?? []
			: package.Nodes;
		AlignLevel(sourceRoots, seriesNode.Children, bindings, allBindings ?? bindings, currentSourceId,
			package.Semantics, seriesNode.Id, null, operations, issues);
		return CreatePlan(runId, targetSeriesId, baseRevision, operations, issues);
	}

	private void AlignLevel(
		IReadOnlyList<SourceNodeDto> incoming,
		IReadOnlyList<BookTreeNodeDto> existing,
		IReadOnlyList<ImportBindingModel> bindings,
		IReadOnlyList<ImportBindingModel> allBindings,
		Guid? currentSourceId,
		ImportSemantics semantics,
		string? parentTargetId,
		string? parentOperationId,
		ICollection<PlanOperationDto> operations,
		ICollection<PlanIssueDto> issues) {
		var incomingNodes = incoming.Select(static node => new IncomingReconciliationNode(
			node.ExternalKey, node.Kind, node.Title, node.TargetId, node.LogicalKey, node.RoleHint, node.OrderHint, node.Content?.ContentHash)).ToList();
		var existingNodes = existing.Select(node => new ExistingReconciliationNode(
			node.Id,
			node.Type == BookTreeNodeType.Chapter ? ReconciliationNodeKind.Content : ReconciliationNodeKind.Container,
			node.Title,
			Order: node.Order,
			BoundExternalKeys: bindings.Where(binding => TargetId(binding) == node.Id)
				.Select(static binding => binding.ExternalNodeKey).ToHashSet(StringComparer.Ordinal))).ToList();
		var result = aligner.Align(incomingNodes, existingNodes);

		foreach (var match in result.Matches) {
			var source = incoming[match.IncomingIndex];
			var target = existing[match.ExistingIndex];
			if (IsNonMaterialized(source.RoleHint)) {
				operations.Add(new PlanOperationDto(
					$"op-{operations.Count + 1}", "RecordEvidence", PlanDisposition.Automatic,
					source.ExternalKey, null,
					$"Record '{source.Title}' as coverage evidence; it aligns near '{target.Title}' but cannot replace or add editorial content.",
					match.Evidence,
					SourceNode: source with { Children = null }));
				if (source.RoleHint?.Contains("placeholder", StringComparison.OrdinalIgnoreCase) == true) {
					issues.Add(new PlanIssueDto("MissingContent",
						"The producer marked this aligned node as a placeholder; descendants are evidence, not chapters.", source.ExternalKey));
				}
				continue;
			}
			var exactBinding = bindings.FirstOrDefault(binding =>
				binding.ExternalNodeKey == source.ExternalKey && TargetId(binding) == target.Id);
			var competingPrimary = IsPrimary(source.RoleHint) && allBindings.Any(binding =>
				binding.ImportSourceId != currentSourceId && TargetId(binding) == target.Id &&
				string.Equals(binding.Role, nameof(CompositionRole.Primary), StringComparison.OrdinalIgnoreCase));
			var (operationType, disposition, contentIssue) = ResolveMatchedContent(source, target, exactBinding, competingPrimary);
			operations.Add(new PlanOperationDto(
				$"op-{operations.Count + 1}", operationType, disposition, source.ExternalKey, target.Id,
				operationType == "NoOp"
					? "Source and editorial nodes are aligned; no content mutation is required."
					: $"Apply incoming content to aligned node '{target.Title}' according to three-way source policy.",
				match.Evidence,
				ParentTargetId: target.ParentId,
				SourceNode: source with { Children = null }));
			if (competingPrimary) {
				issues.Add(new PlanIssueDto("MultiplePrimarySources",
					"Another source is already bound as primary content for this editorial node.", source.ExternalKey));
			}
			if (contentIssue is not null) issues.Add(contentIssue);
			if ((source.Children?.Count ?? 0) > 0 || target.Children.Count > 0) {
				AlignLevel(source.Children ?? [], target.Children, bindings, allBindings, currentSourceId,
					semantics, target.Id, null, operations, issues);
			}
		}

		foreach (var index in result.IncomingOnly) {
			var source = incoming[index];
			if (IsNonMaterialized(source.RoleHint)) {
				operations.Add(new PlanOperationDto(
					$"op-{operations.Count + 1}", "RecordEvidence", PlanDisposition.Automatic,
					source.ExternalKey, null, $"Record '{source.Title}' as source evidence without creating editorial content.",
					SourceNode: source with { Children = null }));
				if (source.RoleHint?.Contains("placeholder", StringComparison.OrdinalIgnoreCase) == true) {
					issues.Add(new PlanIssueDto("MissingContent",
						"The producer marked this node as a placeholder; it is a coverage gap, not a chapter.", source.ExternalKey));
				}
				continue;
			}
			var previousTargetId = result.Matches
				.Where(match => match.IncomingIndex < index)
				.OrderByDescending(static match => match.IncomingIndex)
				.Select(match => existing[match.ExistingIndex].Id)
				.FirstOrDefault();
			var nextTargetId = result.Matches
				.Where(match => match.IncomingIndex > index)
				.OrderBy(static match => match.IncomingIndex)
				.Select(match => existing[match.ExistingIndex].Id)
				.FirstOrDefault();
			// A completely new prefix/suffix has no matched neighbour on one side. Order remains only
			// an insertion hint (never identity evidence), but it can still anchor the new node relative
			// to existing-only siblings such as "Arc 1" before an already present "Arc 2".
			if (source.OrderHint is { } orderHint) {
				previousTargetId ??= existing.Where(node => node.Order < orderHint)
					.OrderByDescending(static node => node.Order).Select(static node => node.Id).FirstOrDefault();
				nextTargetId ??= existing.Where(node => node.Order > orderHint)
					.OrderBy(static node => node.Order).Select(static node => node.Id).FirstOrDefault();
			}
			AddIncomingSubtree(source, parentTargetId, parentOperationId, operations, previousTargetId, nextTargetId);
		}

		if (semantics == ImportSemantics.Snapshot) {
			foreach (var index in result.ExistingOnly) {
				var target = existing[index];
				var policy = ReconciliationPolicy.Evaluate(new PolicyContext(ProposedChangeKind.RemoveFromSnapshot));
				operations.Add(new PlanOperationDto(
					$"op-{operations.Count + 1}", "ArchiveNode", policy.Disposition, string.Empty, target.Id,
					$"'{target.Title}' is absent from a snapshot; archive only after review."));
				issues.Add(new PlanIssueDto(policy.Code, policy.Explanation));
			}
		}
	}

	private static (string Type, PlanDisposition Disposition, PlanIssueDto? Issue) ResolveMatchedContent(
		SourceNodeDto source,
		BookTreeNodeDto target,
		ImportBindingModel? binding,
		bool competingPrimary) {
		if (source.Content is null || target.Type != BookTreeNodeType.Chapter) {
			return ("NoOp", competingPrimary ? PlanDisposition.Review : PlanDisposition.Automatic, null);
		}
		if (string.Equals(source.RoleHint, "supplement", StringComparison.OrdinalIgnoreCase)) {
			var remoteUnchanged = binding is not null && source.Content.ContentHash == binding.LastImportedHash;
			return remoteUnchanged
				? ("NoOp", PlanDisposition.Automatic, null)
				: ("AppendChapterContent", PlanDisposition.Automatic, null);
		}
		if (binding is null) {
			return ("ReplaceChapterContent", PlanDisposition.Review,
				new PlanIssueDto("UnboundReplacement",
					"A heuristic structural match cannot authorize replacing existing prose.", source.ExternalKey));
		}

		var remoteChanged = source.Content.ContentHash != binding.LastImportedHash;
		var localChanged = target.ContentHash != binding.BaseSnapshotKey;
		if (!remoteChanged) return ("NoOp", competingPrimary ? PlanDisposition.Review : PlanDisposition.Automatic, null);
		if (!localChanged && !competingPrimary) return ("ReplaceChapterContent", PlanDisposition.Automatic, null);
		return ("ReplaceChapterContent", PlanDisposition.Review,
			new PlanIssueDto(localChanged ? "ConcurrentLocalAndIncomingEdit" : "MultiplePrimarySources",
				localChanged
					? "Local and incoming content both changed since the last accepted import."
					: "Another source is already primary for this chapter.", source.ExternalKey));
	}

	private static bool IsPrimary(string? role) => !IsNonMaterialized(role) &&
		!string.Equals(role, "supplement", StringComparison.OrdinalIgnoreCase);

	private static bool IsNonMaterialized(string? role) => role is not null &&
		(role.Contains("placeholder", StringComparison.OrdinalIgnoreCase) ||
		 role.Contains("delegation", StringComparison.OrdinalIgnoreCase) ||
		 role.Contains("reference", StringComparison.OrdinalIgnoreCase));

	private static void AddIncomingSubtree(
		SourceNodeDto source,
		string? parentTargetId,
		string? parentOperationId,
		ICollection<PlanOperationDto> operations,
		string? previousTargetId = null,
		string? nextTargetId = null) {
		var policy = ReconciliationPolicy.Evaluate(new PolicyContext(ProposedChangeKind.CreateNode));
		var operationId = $"op-{operations.Count + 1}";
		operations.Add(new PlanOperationDto(
			operationId, "CreateNode", policy.Disposition, source.ExternalKey, null,
			$"Create '{source.Title}' and preserve its source identity.",
			ParentTargetId: parentTargetId,
			ParentOperationId: parentOperationId,
			SourceNode: source with { Children = null },
			PreviousTargetId: previousTargetId,
			NextTargetId: nextTargetId));
		foreach (var child in source.Children ?? []) {
			AddIncomingSubtree(child, null, operationId, operations);
		}
	}

	private static ReconciliationPlanDto CreatePlan(
		Guid runId,
		string? targetSeriesId,
		long? baseRevision,
		IReadOnlyList<PlanOperationDto> operations,
		IReadOnlyList<PlanIssueDto> issues) {
		var summary = new PlanSummaryDto(
			operations.Count(static op => op.Disposition == PlanDisposition.Automatic && op.Type != "NoOp"),
			operations.Count(static op => op.Disposition == PlanDisposition.Review),
			operations.Count(static op => op.Disposition == PlanDisposition.Reject),
			operations.Count(static op => op.Type == "NoOp"));
		return new ReconciliationPlanDto($"imp_{runId}", targetSeriesId, baseRevision, operations, issues, summary);
	}

	private static bool LooksLikeSeries(SourceNodeDto node) =>
		node.Kind == ReconciliationNodeKind.Container &&
		(node.RoleHint?.Contains("series", StringComparison.OrdinalIgnoreCase) == true || node.TargetId?.StartsWith("ser_", StringComparison.Ordinal) == true);

	private static string TargetId(ImportBindingModel binding) => binding.TargetNodeType switch {
		Domain.Entity.Book.BookNodeType.Series => $"ser_{binding.TargetNodeId}",
		Domain.Entity.Book.BookNodeType.Volume => $"vol_{binding.TargetNodeId}",
		Domain.Entity.Book.BookNodeType.Chapter => $"chp_{binding.TargetNodeId}",
		_ => binding.TargetNodeId.ToString()
	};
}
