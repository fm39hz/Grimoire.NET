namespace Grimoire.Infrastructure.Export;

using Application.Export;
using Application.Service.Strategy;
using Common;

/// <summary>
///     Strategy for exporting series to Markdown format using Scriban templates
/// </summary>
public class MarkdownExportStrategy(ITemplateEngine templateEngine) : IExportStrategy {
	public ExportFormat Format => ExportFormat.Markdown;

	public async Task<ExportResult> ExportAsync(BookExportContext context) {
		try {
			var markdown = await templateEngine.RenderAsync("markdown", context);

			var memoryStream = new MemoryStream();
			await using var writer = new StreamWriter(memoryStream, System.Text.Encoding.UTF8, leaveOpen : true);
			await writer.WriteAsync(markdown);
			await writer.FlushAsync();
			memoryStream.Position = 0;

			var fileName = $"{ExportUtilities.SanitizeFileName(context.Series.Title)}.md";

			return ExportResult.Ok(memoryStream, fileName, "text/markdown");
		}
		catch (Exception ex) {
			return ExportResult.Fail(ex.Message);
		}
	}
}
