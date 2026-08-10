namespace Grimoire.Application.Service.Pipeline.Publishing;

using System;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Export;
using Grimoire.Application.Service.Strategy;

/// <summary>
///     Context representing the state of a publishing pipeline run
/// </summary>
public sealed class PublishingContext(Guid seriesId, BinderyRequestDto request) {
	public Guid SeriesId { get; } = seriesId;
	public BinderyRequestDto Request { get; } = request;
	public ExportStructureDto Structure { get; } = request.Structure ?? ExportStructureDefaults.Standard();
	public ExportFormat Format { get; } = request.Format;

	// Context populated during the pipeline execution
	public BookExportContext ExportContext { get; set; } = null!;

	// Final output result
	public ExportResult Result { get; set; } = null!;
}
