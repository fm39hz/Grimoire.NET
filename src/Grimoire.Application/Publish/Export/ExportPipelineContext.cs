namespace Grimoire.Application.Publish.Export;

using System;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Service.Strategy;
using Grimoire.Application.Publish.Dto;

public sealed class ExportPipelineContext
{
    public Guid SeriesId { get; }
    public BinderyRequestDto Request { get; }
    public string JobId { get; }

    // Inter-step State
    public bool SkipExport { get; set; }
    public ExportResult? ExportResult { get; set; }
    public Guid? AssetId { get; set; }
    public JobResult? Result { get; set; }

    // Progress
    public System.Action<int>? OnProgress { get; set; }
    public string? CurrentStage { get; set; }

    public void ReportSubProgress(double fraction)
    {
        OnProgress?.Invoke((int)(fraction * 100));
    }

    public ExportPipelineContext(Guid seriesId, BinderyRequestDto request, string jobId)
    {
        SeriesId = seriesId;
        Request = request;
        JobId = jobId;
    }
}
