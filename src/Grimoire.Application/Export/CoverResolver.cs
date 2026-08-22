namespace Grimoire.Application.Export;

using System.Threading;
using Domain.Common;
using Domain.Entity.Book;
using Dto.Book;
using Microsoft.Extensions.Logging;
using Service.Contract;

/// <summary>
///     Resolves the cover asset used for the package-level EPUB metadata
///     (OPF <c>&lt;meta name="cover"&gt;</c>), preferring the most representative
///     source for the export mode.
/// </summary>
/// <remarks>
///     Source selection by <see cref="BinderyRequestDto.Mode"/>:
///     <list type="bullet">
///         <item><b>Single</b> — the package represents one volume, so the volume's
///         own cover is preferred; falls back to the series cover when the volume
///         has none.</item>
///         <item><b>Anthology</b> (and any other mode) — the package is the whole
///         series, so the series cover is used.</item>
///     </list>
///     OnePerVolume/CustomGroups never reach this resolver directly: they are
///     decomposed into Single requests by <c>ContentGenerationStep</c>.
/// </remarks>
public partial class CoverResolver(
	IAssetService assetService,
	IStorageService storageService,
	ILogger<CoverResolver> logger) {
	public async Task<(AssetModel? asset, Func<Task<Stream?>>? streamProvider)> ResolveAsync(
		SeriesModel series,
		BinderyRequestDto request,
		List<VolumeModel> volumes,
		CancellationToken cancellationToken = default) {
		// Volume-first in Single mode: the package identity is the volume.
		if (string.Equals(request.Mode, "Single", StringComparison.OrdinalIgnoreCase)) {
			var volumeId = ResolveSingleTargetVolumeId(request);
			if (volumeId is { } vid) {
				var volume = volumes.FirstOrDefault(v => v.Id == vid);
				var volumeCoverKey = volume?.Metadata?.CoverImage;
				if (!string.IsNullOrWhiteSpace(volumeCoverKey)) {
					if (await TryResolveKeyAsync(volumeCoverKey, cancellationToken) is { } volCover) {
						return Unpack(volCover);
					}

					LogCoverUnavailable("volume", series.Id, vid, volumeCoverKey, "asset missing or stream unreadable");
				}
				else {
					LogCoverMissing("volume", series.Id, vid);
				}

				// Fall through to series cover.
			}
		}

		// Series-level cover (Anthology path, and Single fallback).
		var seriesCoverKey = series.Metadata?.CoverImage;
		if (string.IsNullOrWhiteSpace(seriesCoverKey)) {
			LogCoverMissing("series", series.Id, null);
			return (null, null);
		}

		if (await TryResolveKeyAsync(seriesCoverKey, cancellationToken) is { } serCover) {
			return Unpack(serCover);
		}

		LogCoverUnavailable("series", series.Id, null, seriesCoverKey, "asset missing or stream unreadable");
		return (null, null);
	}

	/// <summary>
	///     Resolves a cover key (prefixed asset id) to a stream-bearing asset,
	///     probing the backing stream is non-empty before returning it. A fresh
	///     provider is wrapped so the probe's own stream can be disposed safely;
	///     <see cref="IStorageService.GetFileStreamAsync"/> is re-invoked on demand,
	///     which stays correct for non-seekable (S3) streams.
	/// </summary>
	private async Task<ResolvedAsset?> TryResolveKeyAsync(
		string coverKey,
		CancellationToken cancellationToken) {
		if (!PrefixedId.TryToGuid(coverKey, EntityPrefix.Asset, out var id)) {
			return null;
		}

		var asset = await assetService.FindOne(id, cancellationToken);
		if (asset == null) {
			return null;
		}

		if (!await AssetStreamProbe.IsReadableAsync(storageService, id, cancellationToken)) {
			return null;
		}

		return new ResolvedAsset(asset,
			async () => (await storageService.GetFileStreamAsync(id, cancellationToken))?.Stream);
	}

	/// <summary>
	///     Splits a <see cref="ResolvedAsset"/> into the tuple shape <see cref="BookExportContext"/>
	///     expects (eager metadata + lazy stream), keeping internals free of double-nullability.
	/// </summary>
	private static (AssetModel? asset, Func<Task<Stream?>>? streamProvider) Unpack(ResolvedAsset resolved)
		=> (resolved.Asset, resolved.StreamProvider);

	/// <summary>
	///     Extracts the single volume id from a Single-mode request, or null when
	///     the request selects zero or multiple volumes (no single representative).
	/// </summary>
	private static Guid? ResolveSingleTargetVolumeId(BinderyRequestDto request) {
		var ids = request.TargetVolumeIds;
		if (ids is null || ids.Count != 1) {
			return null;
		}

		return PrefixedId.TryToGuid(ids[0], EntityPrefix.Volume, out var volumeId) ? volumeId : null;
	}

	[LoggerMessage(LogLevel.Warning, "Cover unresolved for {Scope} (series {SeriesId}, volume {VolumeId}): {Reason}. Cover key: {CoverKey}")]
	partial void LogCoverUnavailable(string scope, Guid seriesId, Guid? volumeId, string coverKey, string reason);

	[LoggerMessage(LogLevel.Warning, "No cover configured for {Scope} (series {SeriesId}, volume {VolumeId})")]
	partial void LogCoverMissing(string scope, Guid seriesId, Guid? volumeId);
}