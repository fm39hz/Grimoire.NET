namespace Grimoire.Infrastructure.Export.Epub;

using Application.Dto.Book;
using Common;

/// <summary>
///     Processes content/chapters section
/// </summary>
public class ContentSectionProcessor : ISectionProcessor<EpubSectionProcessorContext> {
	public Task ProcessAsync(ExportSectionDto section, EpubSectionProcessorContext context) {
		var chapterIndex = 1;
		var volumeIndex = 1;

		foreach (var volume in context.Volumes) {
			var orderedChapters = context.ChapterMap.TryGetValue(volume.Id, out var chapters)
				? chapters // already ordered and content-loaded
				: [];

			// Create volume XHTML file
			var volumeFileName = $"volume_{volumeIndex:D3}.xhtml";

			// Map cover image if exists
			string? coverImagePath = null;
			if (!string.IsNullOrEmpty(volume.Metadata?.CoverImage) &&
				context.ImageFileMap != null &&
				context.ImageFileMap.TryGetValue(volume.Metadata.CoverImage, out var mappedCoverPath)) {
				coverImagePath = mappedCoverPath;
			}

			var volumeViewModel = new VolumePageViewModel {
				Title = volume.Title,
				FileName = volumeFileName,
				CoverImagePath = coverImagePath,
				PublicationDate = volume.Metadata?.PublicationDate,
				Isbn = !string.IsNullOrEmpty(volume.Metadata?.Isbn)? volume.Metadata.Isbn : null
			};

			var volumeHtml = context.Renderer.RenderVolume(volumeViewModel);
			if (section.CustomCss != null) {
				volumeHtml = HtmlHelper.InjectCustomCss(volumeHtml, section.CustomCss);
			}

			context.PackageBuilder.AddHtmlFile($"OEBPS/{volumeFileName}", volumeHtml);

			var volumeNav = new NavPoint { Title = volume.Title, ContentSrc = volumeFileName, Children = [] };

			foreach (var chapterWithContent in orderedChapters) {
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

				volumeNav.Children.Add(new NavPoint { Title = chapterWithContent.Title, ContentSrc = chapterFileName });

				chapterIndex++;
			}

			if (volumeNav.Children.Count > 0) {
				context.PackageBuilder.AddNavPoint(volumeNav);
			}

			volumeIndex++;
		}

		return Task.CompletedTask;
	}
}
