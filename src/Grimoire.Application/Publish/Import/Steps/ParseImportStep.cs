namespace Grimoire.Application.Publish.Import.Steps;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Import;

public sealed class ParseImportStep : IImportPipelineStep
{
    public int Order => 10;

    public async Task ExecuteAsync(ImportPipelineContext context, CancellationToken cancellationToken)
    {
        context.Normalized = await context.Strategy.ParseAsync(context.SourceStream, cancellationToken);
        context.MergedVolumes = BuildVolumeList(context.Normalized, context.VolumesOverride);
        context.OnProgress?.Invoke(2);
    }

    private static List<NormalizedVolume> BuildVolumeList(
        NormalizedImport normalized,
        List<ImportVolumeDto>? overrideVols)
    {
        if (overrideVols?.Count > 0)
            return overrideVols.Select(v => new NormalizedVolume
            {
                Order = v.Order,
                Title = v.Title ?? normalized.Title,
                Chapters = v.Chapters?.Select(c =>
                {
                    var match = normalized.Volumes
                        .SelectMany(x => x.Chapters)
                        .FirstOrDefault(x => x.Order == c.Order);
                    return new NormalizedChapter
                    {
                        Order = c.Order,
                        Title = c.Title ?? match?.Title ?? $"Chapter {c.Order}",
                        Segments = match?.Segments ?? [],
                        Footnotes = match?.Footnotes ?? [],
                        RawHtml = match?.RawHtml
                    };
                }).ToList() ?? []
            }).ToList();

        return normalized.Volumes;
    }
}
