namespace Grimoire.Infrastructure.Export.Epub;

using Application.Dto.Book;
using Common;

/// <summary>
///     Processes intro/title page section
/// </summary>
public class IntroSectionProcessor : ISectionProcessor<EpubSectionProcessorContext> {
	public Task ProcessAsync(ExportSectionDto section, EpubSectionProcessorContext context) {
		var intro = new IntroPageViewModel {
			Title = context.Series.Title,
			Author = context.Author,
			Tags = context.Series.Metadata?.Tags?.ToList(),
			Description = ExportUtilities.IsSplitDescriptionEnabled(section)
				? null
				: context.Series.Metadata?.Description,
			CoverLocalPath = context.CoverLocalPath,
			CustomCss = section.CustomCss
		};

		var introHtml = context.Renderer.RenderIntro(intro);
		if (section.CustomCss != null) {
			introHtml = HtmlHelper.InjectCustomCss(introHtml, section.CustomCss);
		}

		context.PackageBuilder.AddHtmlFile("OEBPS/intro.xhtml", introHtml);
		context.PackageBuilder.AddNavPoint(new NavPoint {
			Title = EpubConstants.LocalizedText.Introduction, ContentSrc = "intro.xhtml"
		});

		return Task.CompletedTask;
	}
}
