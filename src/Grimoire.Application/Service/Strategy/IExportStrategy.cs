namespace Grimoire.Application.Service.Strategy;

using Domain.Entity.Book;
using Dto.Book;

/// <summary>
///     Strategy interface for exporting series to different formats
/// </summary>
public interface IExportStrategy {
	/// <summary>
	///     The export format this strategy handles
	/// </summary>
	public ExportFormat Format { get; }

	/// <summary>
	///     Exports a series with the specified volumes to the target format
	/// </summary>
	public Task<ExportResult> ExportAsync(SeriesModel series, IEnumerable<VolumeModel> volumes,
		BinderyRequestDto request);
}
