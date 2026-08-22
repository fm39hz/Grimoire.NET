namespace Grimoire.Application.Export;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;
using Service.Contract;

/// <summary>
///     Assembles all data required for any export format.
///     Format strategies must NOT depend on domain services — they receive a BookExportContext instead.
/// </summary>
public class BookExportOrchestrator(
	VolumeResolver volumeResolver,
	ChapterLoader chapterLoader,
	CoverResolver coverResolver,
	ImageAssetCollector imageAssetCollector,
	IBookTreeService bookTreeService,
	ISegmentRepository segmentRepository) {

	public async Task<BookExportContext> BuildContextAsync(
		SeriesModel series,
		BinderyRequestDto request,
		CancellationToken cancellationToken = default) {
		var volumes = await volumeResolver.ResolveAsync(series.Id, request, cancellationToken);
		var chapterMap = await chapterLoader.LoadAsync(volumes, cancellationToken);
		var tree = await bookTreeService.GetTree(series.Id, includeContent: true, cancellationToken);
		var (coverAsset, coverStream) = await coverResolver.ResolveAsync(series, request, volumes, cancellationToken);
		var imageAssets = await imageAssetCollector.CollectAsync(volumes, chapterMap, cancellationToken);
		var assetFileMap = ImageAssetCollector.GenerateFileMap(imageAssets);
		var plainTextDescription = FlattenDescription(series.Metadata?.Description);

		var allChapters = chapterMap.Values.SelectMany(c => c).ToList();
		var segmentsMap = new Dictionary<Guid, List<SegmentModel>>();
		foreach (var ch in allChapters) {
			var segments = (await segmentRepository.FindByChapterPath(ch.Path, cancellationToken)).ToList();
			segmentsMap[ch.Id] = segments;
		}

		return new BookExportContext {
			Series = series,
			Tree = tree,
			Volumes = volumes,
			ChapterMap = chapterMap,
			ChapterSegmentsMap = segmentsMap,
			CoverAsset = coverAsset,
			CoverStreamProvider = coverStream,
			ImageAssets = imageAssets,
			AssetFileMap = assetFileMap,
			PlainTextDescription = plainTextDescription,
			Isbn = ResolveRepresentativeIsbn(request, volumes),
			Structure = request.Structure ?? ExportStructureDefaults.Standard()
		};
	}

	/// <summary>
	///     Resolves the ISBN that represents the whole exported package.
	///     Only Single mode (one volume per EPUB) has a meaningful bibliographic
	///     identity; Anthology compiles many volumes into one package with no
	///     single ISBN, so it omits the identifier entirely.
	/// </summary>
	private static string? ResolveRepresentativeIsbn(BinderyRequestDto request, List<VolumeModel> volumes) {
		if (!string.Equals(request.Mode, "Single", StringComparison.OrdinalIgnoreCase)) {
			return null;
		}

		var targetIds = request.TargetVolumeIds;
		if (targetIds is null || targetIds.Count == 0) {
			return null;
		}

		// OnePerVolume/CustomGroups delegates through Single with exactly one target
		// (see ContentGenerationStep.GenerateBatch). Anthology-style "Single" with
		// multiple targets has no single representative ISBN.
		if (targetIds.Count > 1) {
			return null;
		}

		var target = targetIds[0];
		if (!PrefixedId.TryToGuid(target, EntityPrefix.Volume, out var volumeId)) {
			return null;
		}

		var volume = volumes.FirstOrDefault(v => v.Id == volumeId);
		return NormalizeIsbn(volume?.Metadata?.Isbn);
	}

	/// <summary>Trims and nullifies whitespace-only ISBN values.</summary>
	private static string? NormalizeIsbn(string? isbn) =>
		string.IsNullOrWhiteSpace(isbn) ? null : isbn.Trim();

	private static string? FlattenDescription(List<TextSegmentModel>? description) =>
		description == null || description.Count == 0
			? null
			: string.Join(" ", description.SelectMany(static d => d.Runs.Select(static r => r.Text)));
}
