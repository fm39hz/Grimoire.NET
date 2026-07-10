namespace Grimoire.Application.Service.Pipeline.Publishing;

using System;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Export;
using Grimoire.Application.Service.Strategy;

/// <summary>
///     Context representing the state of a publishing pipeline run
/// </summary>
public sealed class PublishingContext(Guid seriesId, ExportStructureDto structure, ExportFormat format) {
	public Guid SeriesId { get; } = seriesId;
	public ExportStructureDto Structure { get; } = structure;
	public ExportFormat Format { get; } = format;

	// Context populated during the pipeline execution
	public BookExportContext ExportContext { get; set; } = null!;

	// Final output result
	public ExportResult Result { get; set; } = null!;
}
