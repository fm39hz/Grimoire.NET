namespace Grimoire.Application.Export;

using Domain.Entity.Book;
using Dto.Book;

/// <summary>
///     Pure data bag produced by BookExportOrchestrator.
///     Contains everything any export strategy will ever need.
///     No service dependencies — safe to construct in tests with plain objects.
/// </summary>
public class BookExportContext {
	/// <summary>The series being exported.</summary>
	public required SeriesModel Series { get; init; }

	/// <summary>Volumes in ascending Order, filtered per BinderyRequest mode.</summary>
	public required List<VolumeModel> Volumes { get; init; }

	/// <summary>
	///     All chapters with ContentData pre-loaded, grouped by VolumeId,
	///     in ascending Order within each volume.
	/// </summary>
	public required IReadOnlyDictionary<Guid, List<ChapterModel>> ChapterMap { get; init; }

	/// <summary>Cover asset, null when the series has no cover configured.</summary>
	public AssetModel? CoverAsset { get; init; }

	/// <summary>Lazy stream provider for the cover. Null when CoverAsset is null.</summary>
	public Func<Task<Stream?>>? CoverStreamProvider { get; init; }

	/// <summary>
	///     All image assets referenced across every chapter segment.
	///     Key: prefixed asset key string (e.g. "ast_xxxxxxxx-...").
	///     Value: resolved asset metadata + lazy stream provider.
	///     Deduplicated — shared assets appear once regardless of how many chapters reference them.
	/// </summary>
	public required IReadOnlyDictionary<string, ResolvedAsset> ImageAssets { get; init; }

	/// <summary>
	///     Mapping from original asset key (e.g. "ast_...") to a simplified, format-friendly filename (e.g. "img001.jpg").
	/// </summary>
	public required IReadOnlyDictionary<string, string> AssetFileMap { get; init; }

	/// <summary>
	///     A flattened plain-text version of the series description, useful for document metadata.
	/// </summary>
	public string? PlainTextDescription { get; init; }

	/// <summary>Export structure (sections, global CSS) from the original request.</summary>
	public required ExportStructureDto Structure { get; init; }
}
