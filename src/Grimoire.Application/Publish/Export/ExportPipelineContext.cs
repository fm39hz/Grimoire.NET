namespace Grimoire.Application.Publish.Export;

using System;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Publish.Dto;
using Grimoire.Application.Service.Strategy;

public sealed class ExportPipelineContext(Guid seriesId, BinderyRequestDto request, string jobId) {
	public Guid SeriesId { get; } = seriesId;
	public BinderyRequestDto Request { get; } = request;
	public string JobId { get; } = jobId;

	// Inter-step State
	public bool SkipExport { get; set; }
	public ExportResult? ExportResult { get; set; }
	public Guid? AssetId { get; set; }
	public JobResult? Result { get; set; }

	// Progress
	public Action<int>? OnProgress { get; set; }
	public string? CurrentStage { get; set; }

	public void ReportSubProgress(double fraction) => OnProgress?.Invoke((int)(fraction * 100));
}
