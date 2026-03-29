namespace Grimoire.Infrastructure.Export.Common;

using Grimoire.Domain.Entity.Book;

/// <summary>
///     Common context interface for section processing across all export formats
/// </summary>
public interface IBookSectionProcessorContext {
	public SeriesModel Series { get; }
	public List<VolumeModel> Volumes { get; }

	/// <summary>
	///     All chapters with ContentData pre-loaded, grouped by VolumeId.
	///     Removes need for service calls from processors.
	/// </summary>
	public IReadOnlyDictionary<Guid, List<ChapterModel>> ChapterMap { get; }

	public string? Author { get; }
	public string? CoverLocalPath { get; }
	public Dictionary<string, string>? ImageFileMap { get; }
}
