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
            from v in context.BookNodes
            where v.ParentId == seriesId && v.Type == BookNodeType.Volume
            select (DateTime?)v.UpdatedAt
        ).MaxAsync(cancellationToken) ?? DateTime.MinValue;

        var chapterDt = await (
            from c in context.BookNodes
            join v in context.BookNodes on c.ParentId equals v.Id
            where v.ParentId == seriesId && c.Type == BookNodeType.Chapter
            select (DateTime?)c.UpdatedAt
        ).MaxAsync(cancellationToken) ?? DateTime.MinValue;

        var contentDt = await (
            from cc in context.ChapterContents
            join c in context.BookNodes on cc.Id equals c.Id
            join v in context.BookNodes on c.ParentId equals v.Id
            where v.ParentId == seriesId && c.Type == BookNodeType.Chapter
            select (DateTime?)cc.UpdatedAt
        ).MaxAsync(cancellationToken) ?? DateTime.MinValue;

        return new[] { seriesDt, volumeDt, chapterDt, contentDt }.Max();
    }
}
