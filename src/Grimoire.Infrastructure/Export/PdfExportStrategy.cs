namespace Grimoire.Infrastructure.Export;

using Application.Export;
using Application.Service.Strategy;

/// <summary>
///     Strategy for exporting series to PDF format
/// </summary>
public class PdfExportStrategy : IExportStrategy {
	public ExportFormat Format => ExportFormat.Pdf;

	public Task<ExportResult> ExportAsync(BookExportContext context) =>
		// TODO: implement using context
		Task.FromResult(ExportResult.Fail("PDF export not yet implemented"));
}
