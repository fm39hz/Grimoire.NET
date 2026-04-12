namespace Grimoire.Infrastructure.Export;

using Application.Export;
using Application.Service.Strategy;
using Common;

/// <summary>
///     Strategy for exporting series to HTML format using Scriban templates
/// </summary>
public class HtmlExportStrategy(ITemplateEngine templateEngine) : IExportStrategy {
	public ExportFormat Format => ExportFormat.Html;

	public async Task<ExportResult> ExportAsync(BookExportContext context) {
		try {
			var html = await templateEngine.RenderAsync("html", context);

			var memoryStream = new MemoryStream();
			await using var writer = new StreamWriter(memoryStream, System.Text.Encoding.UTF8);
			await writer.WriteAsync(html);
			await writer.FlushAsync();
			memoryStream.Position = 0;

			var fileName = $"{ExportUtilities.SanitizeFileName(context.Series.Title)}.html";

			return ExportResult.Ok(memoryStream, fileName, "text/html");
		}
		catch (Exception ex) {
			return ExportResult.Fail(ex.Message);
		}
	}
}
