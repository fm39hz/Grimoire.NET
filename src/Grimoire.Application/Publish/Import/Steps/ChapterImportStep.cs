namespace Grimoire.Application.Publish.Import.Steps;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Import;

public sealed class ChapterImportStep(
    IChapterImportHandler chapterHandler) : IImportPipelineStep
{
    public int Order => 50;

    public async Task ExecuteAsync(ImportPipelineContext context, CancellationToken cancellationToken)
    {
        if (context.Series is null) return;

        var chaptersCreated = 0;
        var chaptersUpdated = 0;

        foreach (var vol in context.ResolvedVolumes)
        {
            var volEntry = context.MergedVolumes.First(v => v.Order == vol.VolumeOrder);

            foreach (var ch in vol.Chapters)
            {
                var chEntry = volEntry.Chapters.FirstOrDefault(c => c.Order == ch.Order);
                if (chEntry is null) continue;

                var result = await chapterHandler.ImportAsync(
                    vol.Id, chEntry, context.FileMap, cancellationToken);

                if (result.Created) chaptersCreated++;
                else chaptersUpdated++;
            }
        }

        context.ChaptersCreated = chaptersCreated;
        context.ChaptersUpdated = chaptersUpdated;
    }
}
