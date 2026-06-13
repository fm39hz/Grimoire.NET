namespace Grimoire.Application.Publish.Import.Steps;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Import;
using Grimoire.Application.Service.Contract;
using Grimoire.Application.Dto.Book;
using Grimoire.Domain.Common;
using Grimoire.Domain.Entity.Book;
using Grimoire.Domain.Entity.Book.Segment;

public sealed class ChapterImportStep(
    IChapterService chapterService) : IImportPipelineStep
{
    public int Order => 50;

    public async Task ExecuteAsync(ImportPipelineContext context, CancellationToken cancellationToken)
    {
        if (context.Series is null) return;

        var chaptersToImport = new List<(Guid VolumeId, CreateChapterRequestDto Dto)>();

        foreach (var vol in context.ResolvedVolumes)
        {
            var volEntry = context.MergedVolumes.First(v => v.Order == vol.VolumeOrder);

            foreach (var ch in vol.Chapters)
            {
                var chEntry = volEntry.Chapters.FirstOrDefault(c => c.Order == ch.Order);
                if (chEntry is null) continue;

                var segments = RemapImages(chEntry.Segments, context.FileMap);

                var dto = new CreateChapterRequestDto(
                    PrefixedId.ToString(EntityPrefix.Volume, vol.Id),
                    chEntry.Order,
                    chEntry.Title,
                    segments,
                    chEntry.Footnotes,
                    null);

                chaptersToImport.Add((vol.Id, dto));
            }
        }

        if (chaptersToImport.Count == 0) return;

        var (chapters, createdCount, updatedCount) = await chapterService.UpsertBulkAsync(
            context.Series.Id,
            chaptersToImport,
            subProgress =>
            {
                context.ReportSubProgress(subProgress / 100.0);
            },
            cancellationToken);

        context.ChaptersCreated = createdCount;
        context.ChaptersUpdated = updatedCount;
    }

    private static List<SegmentModel> RemapImages(
        List<SegmentModel> segments,
        Dictionary<string, string> assetMap) {

        return segments.Select(s =>
        {
            if (s is ImageSegmentModel img && assetMap.TryGetValue(img.AssetKey, out var key))
                return img with { AssetKey = key };
            return s;
        }).ToList();
    }
}
