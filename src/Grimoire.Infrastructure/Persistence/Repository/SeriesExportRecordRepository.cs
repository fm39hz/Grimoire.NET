namespace Grimoire.Infrastructure.Persistence.Repository;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;

public sealed class SeriesExportRecordRepository(ApplicationDbContext context)
    : CrudRepository<SeriesExportRecord>(context), ISeriesExportRecordRepository
{
    public async Task<SeriesExportRecord?> GetBySeriesAndFormatAsync(Guid seriesId, string format,
        CancellationToken cancellationToken = default) =>
        await Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SeriesId == seriesId && r.Format == format, cancellationToken);

    public async Task<DateTime> GetMaxContentTimestampAsync(Guid seriesId, CancellationToken cancellationToken = default)
    {
        LTree seriesPath = "n" + seriesId.ToString("N");

        var seriesDt = await Context.Series
            .Where(s => s.Id == seriesId)
            .Select(s => s.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var volumeDt = await Context.Volumes
            .Where(v => v.Path.MatchesLQuery($"{seriesPath}.*"))
            .Select(v => (DateTime?)v.UpdatedAt)
            .MaxAsync(cancellationToken) ?? DateTime.MinValue;

        var chapterDt = await Context.Chapters
            .Where(c => c.Path.MatchesLQuery($"{seriesPath}.*.*"))
            .Select(c => (DateTime?)c.UpdatedAt)
            .MaxAsync(cancellationToken) ?? DateTime.MinValue;

        var segmentDt = await Context.Segments
            .Where(s => s.Path.IsDescendantOf(seriesPath))
            .Select(s => (DateTime?)s.UpdatedAt)
            .MaxAsync(cancellationToken) ?? DateTime.MinValue;

        return new[] { seriesDt, volumeDt, chapterDt, segmentDt }.Max();
    }
}
