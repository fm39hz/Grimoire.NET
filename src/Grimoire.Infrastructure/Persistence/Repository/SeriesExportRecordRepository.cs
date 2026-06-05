namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;
using System.Threading;

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
        var seriesDt = await (
            from s in context.Series
            where s.Id == seriesId
            select s.UpdatedAt
        ).FirstOrDefaultAsync(cancellationToken);

        var volumeDt = await (
            from v in context.Volumes
            where v.SeriesId == seriesId
            select (DateTime?)v.UpdatedAt
        ).MaxAsync(cancellationToken) ?? DateTime.MinValue;

        var chapterDt = await (
            from c in context.Chapters
            join v in context.Volumes on c.VolumeId equals v.Id
            where v.SeriesId == seriesId
            select (DateTime?)c.UpdatedAt
        ).MaxAsync(cancellationToken) ?? DateTime.MinValue;

        var contentDt = await (
            from cc in context.ChapterContents
            join c in context.Chapters on cc.Id equals c.Id
            join v in context.Volumes on c.VolumeId equals v.Id
            where v.SeriesId == seriesId
            select (DateTime?)cc.UpdatedAt
        ).MaxAsync(cancellationToken) ?? DateTime.MinValue;

        return new[] { seriesDt, volumeDt, chapterDt, contentDt }.Max();
    }
}
