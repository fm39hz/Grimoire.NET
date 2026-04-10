namespace Grimoire.Infrastructure.Export.Epub;

using Application.Dto.Book;
using Common;

/// <summary>
///     Processes description section (separate from intro)
/// </summary>
public class DescriptionSectionProcessor : ISectionProcessor<EpubSectionProcessorContext> {
	public Task ProcessAsync(ExportSectionDto section, EpubSectionProcessorContext context) {
		if (context.Series.Metadata?.Description == null || context.Series.Metadata.Description.Count == 0) {
			return Task.CompletedTask;
		}

		var intro = new IntroPageViewModel {
			Title = context.Series.Title,
			Author = context.Author,
			Tags = null,
			Description = context.Series.Metadata.Description,
			CoverLocalPath = null,
			CustomCss = section.CustomCss
		};

		var descHtml = context.Renderer.RenderIntro(intro);
		if (section.CustomCss != null) {
			descHtml = HtmlHelper.InjectCustomCss(descHtml, section.CustomCss);
		}

		context.PackageBuilder.AddHtmlFile("OEBPS/description.xhtml", descHtml);
		context.PackageBuilder.AddNavPoint(new NavPoint {
			Title = EpubConstants.LocalizedText.Summary, ContentSrc = "description.xhtml"
		});

		return Task.CompletedTask;
	}
}
