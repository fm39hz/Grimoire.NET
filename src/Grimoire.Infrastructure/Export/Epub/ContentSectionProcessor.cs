namespace Grimoire.Infrastructure.Export.Epub;

using Application.Dto.Book;
using Application.Service.Contract;

/// <summary>
///     Processes content/chapters section
/// </summary>
public class ContentSectionProcessor(
	IChapterService chapterService,
	IVolumeService volumeService) : ISectionProcessor {

	public async Task ProcessAsync(ExportSectionDto section, SectionProcessorContext context) {
		var chapterIndex = 1;

		foreach (var volume in context.Volumes) {
			var chapters = await volumeService.FindAllChapters(volume.Id);
			var orderedChapters = chapters.OrderBy(c => c.Order).ToList();

			var volumeNav = new NavPoint {
				Title = volume.Title,
				ContentSrc = $"chapter_{chapterIndex:D3}.xhtml",
				Children = []
			};

			foreach (var chapter in orderedChapters) {
				var chapterWithContent = await chapterService.FindOne(chapter.Id);
				if (chapterWithContent?.ContentData == null) {
					continue;
				}

				var chapterFileName = $"chapter_{chapterIndex:D3}.xhtml";

				var chapterViewModel = new ChapterPageViewModel {
					Title = chapterWithContent.Title,
					Segments = chapterWithContent.ContentData.Segments,
					Footnotes = chapterWithContent.ContentData.Footnotes,
					FileName = chapterFileName,
					ImageFileMap = context.ImageFileMap
				};

				var chapterHtml = context.Renderer.RenderChapter(chapterViewModel);
				if (section.CustomCss != null) {
					chapterHtml = HtmlHelper.InjectCustomCss(chapterHtml, section.CustomCss);
				}

				context.PackageBuilder.AddHtmlFile($"OEBPS/{chapterFileName}", chapterHtml);

				volumeNav.Children.Add(new NavPoint {
					Title = chapterWithContent.Title,
					ContentSrc = chapterFileName
				});

				chapterIndex++;
			}

			if (volumeNav.Children.Count > 0) {
				context.PackageBuilder.AddNavPoint(volumeNav);
			}
		}
	}
}
