namespace Grimoire.Domain.Common.Repository;

using System.Threading;
using Entity.Book;

public interface ISeriesExportRecordRepository : IRepository<SeriesExportRecord>
{
    Task<SeriesExportRecord?> GetBySeriesAndFormatAsync(Guid seriesId, string format, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns the latest UpdatedAt across the entire content tree for a series
    ///     (series itself, volumes, chapters, chapter contents).
    ///     Used to determine if source content changed since last export.
    /// </summary>
    Task<DateTime> GetMaxContentTimestampAsync(Guid seriesId, CancellationToken cancellationToken = default);
}
