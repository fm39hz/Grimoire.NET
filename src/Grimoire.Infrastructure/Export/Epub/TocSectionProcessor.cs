namespace Grimoire.Infrastructure.Export.Epub;

using Application.Dto.Book;
using Common;

/// <summary>
///     Processes table of contents section
/// </summary>
public class TocSectionProcessor : ISectionProcessor {
	public Task ProcessAsync(ExportSectionDto section, SectionProcessorContext context) {
		var navHtml = context.Renderer.RenderToc(context.PackageBuilder.GetNavPoints());

		if (section.CustomCss != null) {
			navHtml = HtmlHelper.InjectCustomCss(navHtml, section.CustomCss);
		}

		context.PackageBuilder.AddHtmlFile(EpubConstants.Paths.NavFile, navHtml);

		return Task.CompletedTask;
	}
}
