namespace Grimoire.Infrastructure.Export;

using Application.Dto.Book;
using Application.Service.Strategy;
using Domain.Entity.Book;

/// <summary>
///     Strategy for exporting series to PDF format
/// </summary>
public class PdfExportStrategy : IExportStrategy {
	public ExportFormat Format => ExportFormat.Pdf;

	public async Task<ExportResult> ExportAsync(
		SeriesModel series,
		IEnumerable<VolumeModel> volumes,
		BinderyRequestDto request) {
		try {
			// TODO: Implement PDF generation
			// Consider using a library like iTextSharp, PdfSharp, or QuestPDF

			var memoryStream = new MemoryStream();
			await using var writer = new StreamWriter(memoryStream);
			await writer.WriteAsync($"PDF export for series: {series.Title}");
			await writer.FlushAsync();
			memoryStream.Position = 0;

			var fileName = $"{SanitizeFileName(series.Title)}.pdf";

			return new ExportResult {
				ContentStream = memoryStream,
				FileName = fileName,
				ContentType = "application/pdf",
				Success = true
			};
		}
		catch (Exception ex) {
			return new ExportResult {
				ContentStream = Stream.Null,
				FileName = string.Empty,
				ContentType = string.Empty,
				Success = false,
				ErrorMessage = ex.Message
			};
		}
	}

	private static string SanitizeFileName(string fileName) {
		var invalidChars = Path.GetInvalidFileNameChars();
		return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
	}
}
