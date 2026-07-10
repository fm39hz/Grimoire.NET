namespace Grimoire.Application.Service.Implementation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contract;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Common.ValueObject;

public sealed class AssetOwnershipService(
	ISeriesRepository seriesRepository,
	IVolumeRepository volumeRepository,
	ISegmentRepository segmentRepository,
	IAssetRepository assetRepository) : IAssetOwnershipService {

	public async Task ReconcileSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default) {
		var series = await seriesRepository.FindOne(seriesId, cancellationToken);
		if (series is null || series.Path.Level == 0) {
			return;
		}

		var seriesPath = series.Path;
		var volumes = (await volumeRepository.FindBySeriesId(seriesId, cancellationToken)).ToList();

		// Collect image segments under this series path
		var imageSegments = (await segmentRepository.FindImageSegmentsBySeriesPath(seriesPath, cancellationToken)).ToList();

		// Map asset ID to lists of usage path BookPath objects
		var usagePathsByAssetId = new Dictionary<Guid, List<BookPath>>();

		// 1. Series Cover usage
		AddUsagePath(usagePathsByAssetId, series.Metadata?.CoverImage, seriesPath);

		// 2. Volume Cover usages
		foreach (var volume in volumes) {
			AddUsagePath(usagePathsByAssetId, volume.Metadata?.CoverImage, volume.Path);
		}

		// 3. Image Segment usages
		foreach (var segment in imageSegments) {
			// A segment's path is s.v.c.seg, we normalize it to chapter level (s.v.c)
			if (segment.Path.Level >= 3) {
				var chapterPath = segment.Path.GetSubpath(0, 3);
				AddUsagePath(usagePathsByAssetId, segment.AssetKey, chapterPath);
			}
		}

		if (usagePathsByAssetId.Count == 0) {
			return;
		}

		var assets = await assetRepository.FindByIdsAsync(usagePathsByAssetId.Keys, cancellationToken);

		foreach (var (assetId, paths) in usagePathsByAssetId) {
			if (!assets.TryGetValue(assetId, out var asset)) {
				continue;
			}

			var lcaPath = BookPath.FindLowestCommonAncestor(paths);
			if (lcaPath is null || lcaPath.Value.Level == 0) {
				continue;
			}

			var ownerGuid = lcaPath.Value.GetLastNodeGuid();
			if (ownerGuid != Guid.Empty) {
				if (asset.OwnerNodeId == ownerGuid) {
					continue;
				}
				asset.OwnerNodeId = ownerGuid;
				await assetRepository.Update(asset, cancellationToken);
			}
		}
	}

	private static void AddUsagePath(Dictionary<Guid, List<BookPath>> result, string? assetKey, BookPath path) {
		if (!PrefixedId.TryToGuid(assetKey, EntityPrefix.Asset, out var assetId)) {
			return;
		}

		if (!result.TryGetValue(assetId, out var paths)) {
			paths = [];
			result[assetId] = paths;
		}

		paths.Add(path);
	}
}
