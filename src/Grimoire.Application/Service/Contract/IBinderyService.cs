namespace Grimoire.Application.Service.Contract;

using System.Threading;
using Dto.Book;
using Strategy;

/// <summary>
///     Service for exporting series with custom structure and layout
/// </summary>
public interface IBinderyService {
	/// <summary>
	///     Export a series with custom structure and layout
	/// </summary>
	public Task<ExportResult> ExportSeriesAsync(Guid seriesId, BinderyRequestDto request, CancellationToken cancellationToken = default);
}
