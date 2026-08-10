namespace Grimoire.Tests.Application;

using Grimoire.Application.Dto.Book.Tree;
using Grimoire.Application.Ingestion.Analysis;
using Grimoire.Application.Ingestion.Contract;
using Grimoire.Application.Ingestion.Reconciliation;
using Grimoire.Domain.Entity.Ingestion;
using Grimoire.Domain.Entity.Book;
using Xunit;

public sealed class SourcePackagePlanBuilderTests {
	private readonly SourcePackagePlanBuilder builder = new(new SiblingSequenceAligner(new NodeMatchScorer()));

	[Fact]
	public void Patch_OmissionDoesNotProposeRemoval() {
		var plan = builder.Build(Guid.NewGuid(), Package(ImportSemantics.Patch), "ser_11111111-1111-1111-1111-111111111111", 4, Tree(), []);

		Assert.DoesNotContain(plan.Operations, static operation => operation.Type == "ArchiveNode");
	}

	[Fact]
	public void Snapshot_OmissionProposesReviewedArchive() {
		var plan = builder.Build(Guid.NewGuid(), Package(ImportSemantics.Snapshot), "ser_11111111-1111-1111-1111-111111111111", 4, Tree(), []);

		var removal = Assert.Single(plan.Operations, static operation => operation.Type == "ArchiveNode");
		Assert.Equal(PlanDisposition.Review, removal.Disposition);
		Assert.Contains(plan.Issues, static issue => issue.Code == "SnapshotRemoval");
	}

	[Fact]
	public void MissingTargetProducesExplicitIssueAndNoMutation() {
		var plan = builder.Build(Guid.NewGuid(), Package(ImportSemantics.Patch), null, null, null, []);

		Assert.Empty(plan.Operations);
		Assert.Contains(plan.Issues, static issue => issue.Code == "TargetRequired");
	}

	[Fact]
	public void HakoPlaceholderIsCoverageEvidence_NotEditorialContent() {
		var package = PackageWithNodes([
			new SourceNodeDto("arc-1-placeholder", ReconciliationNodeKind.Container, "Arc 1 - do nhóm khác dịch",
				RoleHint: "placeholder", OrderHint: 1,
				Relations: [new SourceRelationDto("delegates-to", Uri: "https://docln.sbs/truyen/8306")]),
			new SourceNodeDto("arc-2", ReconciliationNodeKind.Container, "Arc 2", OrderHint: 2,
				Children: [new SourceNodeDto("arc-2-chapter-1", ReconciliationNodeKind.Content, "Chương 1", OrderHint: 1)])
		]);

		var plan = builder.Build(Guid.NewGuid(), package, SeriesId, 0, EmptyTree(), []);

		Assert.Contains(plan.Operations, static operation => operation.Type == "RecordEvidence" && operation.ExternalNodeKey == "arc-1-placeholder");
		Assert.DoesNotContain(plan.Operations, static operation => operation.Type == "CreateNode" && operation.ExternalNodeKey == "arc-1-placeholder");
		Assert.Contains(plan.Issues, static issue => issue.Code == "MissingContent");
		Assert.Contains(plan.Operations, static operation => operation.Type == "CreateNode" && operation.ExternalNodeKey == "arc-2");
	}

	[Fact]
	public void PlaceholderAlignedToExistingArcDoesNotMaterializeNoticeChildren() {
		var package = PackageWithNodes([
			new SourceNodeDto("arc-1-placeholder", ReconciliationNodeKind.Container, "Arc 1",
				LogicalKey: "work:arc:1", RoleHint: "placeholder", OrderHint: 1,
				Children: [new SourceNodeDto("translation-notice", ReconciliationNodeKind.Content, "Do nhóm khác dịch", OrderHint: 1)])
		]);
		var tree = EmptyTree();
		tree.Root.Children[0].Children.Add(new BookTreeNodeDto {
			Id = "vol_22222222-2222-2222-2222-222222222222",
			Type = BookTreeNodeType.Volume,
			Title = "Arc 1",
			Order = 1,
			ParentId = SeriesId
		});

		var plan = builder.Build(Guid.NewGuid(), package, SeriesId, 0, tree, []);

		Assert.Contains(plan.Operations, static operation => operation.Type == "RecordEvidence" && operation.ExternalNodeKey == "arc-1-placeholder");
		Assert.DoesNotContain(plan.Operations, static operation => operation.ExternalNodeKey == "translation-notice");
	}

	[Fact]
	public void ActualArcOneFromSecondSourceIsInsertedBeforeExistingArcTwo() {
		var package = PackageWithNodes([
			new SourceNodeDto("arc-1", ReconciliationNodeKind.Container, "Arc 1", OrderHint: 1,
				Children: [new SourceNodeDto("arc-1-chapter-1", ReconciliationNodeKind.Content, "Chương 1", OrderHint: 1)])
		]);
		var tree = EmptyTree();
		tree.Root.Children[0].Children.Add(new BookTreeNodeDto {
			Id = "vol_22222222-2222-2222-2222-222222222222",
			Type = BookTreeNodeType.Volume,
			Title = "Arc 2",
			Order = 2,
			ParentId = SeriesId
		});

		var plan = builder.Build(Guid.NewGuid(), package, SeriesId, 0, tree, []);

		var create = Assert.Single(plan.Operations, static operation => operation.ExternalNodeKey == "arc-1");
		Assert.Equal("vol_22222222-2222-2222-2222-222222222222", create.NextTargetId);
	}

	[Fact]
	public void BoundRemoteUpdateIsAutomaticOnlyWhileLocalEqualsAcceptedBase() {
		var sourceId = Guid.NewGuid();
		var chapterId = Guid.Parse("11111111-1111-1111-1111-111111111113");
		var binding = new ImportBindingModel {
			ImportSourceId = sourceId,
			ExternalNodeKey = "chapter-1",
			TargetNodeId = chapterId,
			TargetNodeType = BookNodeType.Chapter,
			Role = "Primary",
			LastImportedHash = "remote-base",
			BaseSnapshotKey = "local-base"
		};
		var package = PackageWithNodes([
			new SourceNodeDto("volume-1", ReconciliationNodeKind.Container, "Volume 1", OrderHint: 1,
				Children: [new SourceNodeDto("chapter-1", ReconciliationNodeKind.Content, "Chapter 1", OrderHint: 1,
					Content: new SourceContentDto(SourceContentFormat.Markdown, Inline: "new", ContentHash: "remote-new"))])
		]);
		var cleanTree = TreeWithContentHash("local-base");
		var editedTree = TreeWithContentHash("local-edited");

		var safe = builder.Build(Guid.NewGuid(), package, SeriesId, 4, cleanTree, [binding], sourceId, [binding]);
		var conflict = builder.Build(Guid.NewGuid(), package, SeriesId, 4, editedTree, [binding], sourceId, [binding]);

		var safeReplace = Assert.Single(safe.Operations, static operation => operation.Type == "ReplaceChapterContent");
		Assert.Equal(PlanDisposition.Automatic, safeReplace.Disposition);
		var reviewedReplace = Assert.Single(conflict.Operations, static operation => operation.Type == "ReplaceChapterContent");
		Assert.Equal(PlanDisposition.Review, reviewedReplace.Disposition);
		Assert.Contains(conflict.Issues, static issue => issue.Code == "ConcurrentLocalAndIncomingEdit");
	}

	private const string SeriesId = "ser_11111111-1111-1111-1111-111111111111";

	private static SourcePackageDto PackageWithNodes(IReadOnlyList<SourceNodeDto> nodes) => new(
		"1.0", new SourceProducerDto("hako-crawler", "1"), "golden-run",
		new SourceDescriptorDto("https://docln.sbs/truyen/11662", "docln"),
		new TargetHintDto(SeriesId, Titles: ["Isekai demo Bunan ni Ikitai Shoukougun"], Authors: ["Antai"]),
		ImportSemantics.Patch, nodes);

	private static BookTreeDto EmptyTree() => new(new BookTreeNodeDto {
		Id = "bookshelf:default", Type = BookTreeNodeType.BookShelf, Title = "Shelf",
		Children = [new BookTreeNodeDto { Id = SeriesId, Type = BookTreeNodeType.Series, Title = "Series" }]
	});

	private static BookTreeDto TreeWithContentHash(string hash) {
		var tree = Tree();
		tree.Root.Children[0].Children[0].Children[0] = tree.Root.Children[0].Children[0].Children[0] with { ContentHash = hash };
		return tree;
	}

	private static SourcePackageDto Package(ImportSemantics semantics) => new(
		"1.0",
		new SourceProducerDto("test-producer", "1"),
		"run-1",
		new SourceDescriptorDto("series-1", "test"),
		new TargetHintDto("ser_11111111-1111-1111-1111-111111111111"),
		semantics,
		[
			new SourceNodeDto("volume-1", ReconciliationNodeKind.Container, "Volume 1", OrderHint: 1,
				Children: [new SourceNodeDto("chapter-1", ReconciliationNodeKind.Content, "Chapter 1", OrderHint: 1)])
		]);

	private static BookTreeDto Tree() => new(new BookTreeNodeDto {
		Id = "bookshelf:default",
		Type = BookTreeNodeType.BookShelf,
		Title = "Shelf",
		Children = [
			new BookTreeNodeDto {
				Id = "ser_11111111-1111-1111-1111-111111111111",
				Type = BookTreeNodeType.Series,
				Title = "Series",
				Children = [
					new BookTreeNodeDto {
						Id = "vol_11111111-1111-1111-1111-111111111112",
						Type = BookTreeNodeType.Volume,
						Title = "Volume 1",
						Order = 1,
						Children = [
							new BookTreeNodeDto {
								Id = "chp_11111111-1111-1111-1111-111111111113",
								Type = BookTreeNodeType.Chapter,
								Title = "Chapter 1",
								Order = 1
							},
							new BookTreeNodeDto {
								Id = "chp_11111111-1111-1111-1111-111111111114",
								Type = BookTreeNodeType.Chapter,
								Title = "Chapter 2",
								Order = 2
							}
						]
					}
				]
			}
		]
	});
}
