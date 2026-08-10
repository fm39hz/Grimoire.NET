namespace Grimoire.Application.Ingestion.Contract;

using FluentValidation;

public sealed class SourcePackageDtoValidator : AbstractValidator<SourcePackageDto> {
	public SourcePackageDtoValidator() {
		RuleFor(static package => package.SchemaVersion).Equal("1.0");
		RuleFor(static package => package.Producer.Id).NotEmpty().MaximumLength(200);
		RuleFor(static package => package.Producer.Version).NotEmpty().MaximumLength(100);
		RuleFor(static package => package.IdempotencyKey).NotEmpty().MaximumLength(500);
		RuleFor(static package => package.Source.ExternalKey).NotEmpty().MaximumLength(1000);
		RuleFor(static package => package.Source.Provider).NotEmpty().MaximumLength(200);
		RuleFor(static package => package.Nodes).NotNull().NotEmpty();
		RuleFor(static package => package).Custom(ValidateTree);
	}

	private static void ValidateTree(SourcePackageDto package, ValidationContext<SourcePackageDto> context) {
		var keys = new HashSet<string>(StringComparer.Ordinal);
		var knownRelations = new List<(string NodeKey, SourceRelationDto Relation)>();
		foreach (var root in package.Nodes) Visit(root, keys, knownRelations, context);

		foreach (var (nodeKey, relation) in knownRelations) {
			if (relation.TargetExternalKey is not null && !keys.Contains(relation.TargetExternalKey)) {
				context.AddFailure(nameof(package.Nodes),
					$"Node '{nodeKey}' relates to missing external key '{relation.TargetExternalKey}'.");
			}
		}

		var assetKeys = new HashSet<string>(StringComparer.Ordinal);
		foreach (var asset in package.Assets ?? []) {
			if (string.IsNullOrWhiteSpace(asset.ExternalKey) || !assetKeys.Add(asset.ExternalKey)) {
				context.AddFailure(nameof(package.Assets), $"Asset external key '{asset.ExternalKey}' is empty or duplicated.");
			}
			if (string.IsNullOrWhiteSpace(asset.StagingObjectKey) || string.IsNullOrWhiteSpace(asset.ContentHash)) {
				context.AddFailure(nameof(package.Assets), $"Asset '{asset.ExternalKey}' requires stagingObjectKey and contentHash.");
			}
		}
	}

	private static void Visit(
		SourceNodeDto node,
		ISet<string> keys,
		ICollection<(string NodeKey, SourceRelationDto Relation)> relations,
		ValidationContext<SourcePackageDto> context) {
		if (string.IsNullOrWhiteSpace(node.ExternalKey) || !keys.Add(node.ExternalKey)) {
			context.AddFailure(nameof(SourcePackageDto.Nodes), $"Node external key '{node.ExternalKey}' is empty or duplicated.");
		}
		if (string.IsNullOrWhiteSpace(node.Title)) {
			context.AddFailure(nameof(SourcePackageDto.Nodes), $"Node '{node.ExternalKey}' requires a title.");
		}
		if (node.Content is { Inline: null, StagingObjectKey: null }) {
			context.AddFailure(nameof(SourcePackageDto.Nodes), $"Node '{node.ExternalKey}' content requires inline data or a staging object key.");
		}
		foreach (var relation in node.Relations ?? []) relations.Add((node.ExternalKey, relation));
		foreach (var child in node.Children ?? []) Visit(child, keys, relations, context);
	}
}
