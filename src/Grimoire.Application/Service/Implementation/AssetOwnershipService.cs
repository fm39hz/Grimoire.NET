namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;

public sealed class AssetOwnershipService(
	IBookTreeRepository treeRepository,
	ISeriesRepository seriesRepository,
	IVolumeRepository volumeRepository,
	IChapterRepository chapterRepository,
	IAssetRepository assetRepository) : IAssetOwnershipService {
	public async Task ReconcileSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default) {
		var nodes = (await treeRepository.FindSeriesTree(seriesId, cancellationToken)).ToList();
		if (nodes.Count == 0) {
			return;
		}

		var nodesById = nodes.ToDictionary(n => n.Id);
		var usageByAssetId = await CollectUsages(seriesId, nodes, cancellationToken);
		if (usageByAssetId.Count == 0) {
			return;
		}

		var assets = await assetRepository.FindByIdsAsync(usageByAssetId.Keys, cancellationToken);
		foreach (var (assetId, usageNodeIds) in usageByAssetId) {
			if (!assets.TryGetValue(assetId, out var asset)) {
				continue;
			}

			var ownerCandidates = usageNodeIds.Where(nodesById.ContainsKey).Distinct().ToList();
			var newOwnerId = ResolveOwner(asset, ownerCandidates, nodesById);
			if (asset.OwnerNodeId == newOwnerId) {
				continue;
			}

			asset.OwnerNodeId = newOwnerId;
			await assetRepository.Update(asset, cancellationToken);
		}
	}

	private async Task<Dictionary<Guid, List<Guid>>> CollectUsages(
		Guid seriesId,
		IReadOnlyList<BookNodeModel> nodes,
		CancellationToken cancellationToken) {
		var result = new Dictionary<Guid, List<Guid>>();

		var series = await seriesRepository.FindOne(seriesId, cancellationToken);
		AddUsage(result, series?.Metadata?.CoverImage, seriesId);

		var volumeIds = nodes.Where(n => n.Type == BookNodeType.Volume).Select(n => n.Id).ToList();
		var volumes = await volumeRepository.FindBySeriesId(seriesId, cancellationToken);
		foreach (var volume in volumes.Where(v => volumeIds.Contains(v.Id))) {
			AddUsage(result, volume.Metadata?.CoverImage, volume.Id);
		}

		var chapters = await chapterRepository.FindByVolumeIdsWithContent(volumeIds, cancellationToken);
		foreach (var chapter in chapters) {
			foreach (var segment in chapter.ContentData?.Segments.OfType<ImageSegmentModel>() ?? []) {
				AddUsage(result, segment.AssetKey, chapter.Id);
			}
		}

		return result;
	}

	private static Guid? ResolveOwner(
		AssetModel asset,
		List<Guid> usageNodeIds,
		Dictionary<Guid, BookNodeModel> nodesById) {
		if (asset.OwnerNodeId is null) {
			return null;
		}

		if (!nodesById.ContainsKey(asset.OwnerNodeId.Value) && asset.OwnerNodeId != Guid.Empty) {
			return null;
		}

		if (usageNodeIds.Count == 0) {
			return asset.OwnerNodeId;
		}

		return FindLowestCommonAncestor(usageNodeIds, nodesById);
	}

	private static Guid? FindLowestCommonAncestor(
		IReadOnlyList<Guid> nodeIds,
		Dictionary<Guid, BookNodeModel> nodesById) {
		var ancestorPaths = nodeIds
			.Select(id => GetAncestorPath(id, nodesById))
			.Where(path => path.Count > 0)
			.ToList();

		if (ancestorPaths.Count == 0) {
			return null;
		}

		var common = ancestorPaths
			.Skip(1)
			.Aggregate(
				ancestorPaths[0].ToHashSet(),
				(set, path) => {
					set.IntersectWith(path);
					return set;
				});

		return ancestorPaths[0].FirstOrDefault(common.Contains);
	}

	private static List<Guid> GetAncestorPath(Guid nodeId, Dictionary<Guid, BookNodeModel> nodesById) {

		var result = new List<Guid>();
		var visited = new HashSet<Guid>();
		var currentId = nodeId;
		while (nodesById.TryGetValue(currentId, out var node) && visited.Add(node.Id)) {
			result.Add(node.Id);
			if (node.ParentId is null) {
				break;
			}

			currentId = node.ParentId.Value;
		}

		return result;
	}

	private static void AddUsage(Dictionary<Guid, List<Guid>> result, string? assetKey, Guid nodeId) {
		if (!PrefixedId.TryToGuid(assetKey, EntityPrefix.Asset, out var assetId)) {
			return;
		}

		if (!result.TryGetValue(assetId, out var usageNodeIds)) {
			usageNodeIds = [];
			result[assetId] = usageNodeIds;
		}

		usageNodeIds.Add(nodeId);
	}
}
