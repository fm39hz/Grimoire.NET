namespace Grimoire.Application.Service.Strategy;

using System.Threading;
using Export;

/// <summary>
///     Strategy interface for exporting series to different formats
/// </summary>
public interface IExportStrategy {
	/// <summary>
	///     The export format this strategy handles
	/// </summary>
	public ExportFormat Format { get; }

	/// <summary>
	///     Exports a series using pre-assembled export context
	/// </summary>
	public Task<ExportResult> ExportAsync(BookExportContext context, CancellationToken cancellationToken = default);
}
