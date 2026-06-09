namespace Grimoire.Application.Publish.Import;

using System.Collections.Generic;
using System.IO;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Import;
using Grimoire.Application.Publish.Dto;
using Grimoire.Domain.Entity.Book;

public sealed class ImportPipelineContext
{
    // Inputs
    public IImportStrategy Strategy { get; }
    public CreateSeriesRequestDto SeriesDto { get; }
    public List<ImportVolumeDto>? VolumesOverride { get; }
    public Stream SourceStream { get; }
    public string JobId { get; }

    // Inter-step State
    public NormalizedImport? Normalized { get; set; }
    public List<NormalizedVolume> MergedVolumes { get; set; } = [];
    public SeriesModel? Series { get; set; }
    public Dictionary<string, string> FileMap { get; set; } = [];
    public List<ResolvedVolume> ResolvedVolumes { get; set; } = [];
    public int ChaptersCreated { get; set; }
    public int ChaptersUpdated { get; set; }
    
    // Output
    public JobResult? Result { get; set; }

    public ImportPipelineContext(
        IImportStrategy strategy,
        CreateSeriesRequestDto seriesDto,
        List<ImportVolumeDto>? volumesOverride,
        Stream sourceStream,
        string jobId)
    {
        Strategy = strategy;
        SeriesDto = seriesDto;
        VolumesOverride = volumesOverride;
        SourceStream = sourceStream;
        JobId = jobId;
    }
}
