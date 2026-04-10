namespace Grimoire.Infrastructure.Export;

using Application.Dto.Book;
using Application.Export;
using Application.Extensions;
using Application.Service.Strategy;
using Common;
using Domain.Entity.Book.Segment;
using Epub;
using Microsoft.Extensions.Logging;

/// <summary>
///     Strategy for exporting series to EPUB format
/// </summary>
public partial class EpubExportStrategy(
	ILogger<EpubExportStrategy> logger,
	ISectionProcessorFactory<EpubSectionProcessorContext> sectionProcessorFactory) : IExportStrategy {
	public ExportFormat Format => ExportFormat.Epub;

	public async Task<ExportResult> ExportAsync(BookExportContext context) {
		try {
			var packageBuilder = new EpubPackageBuilder();
			var renderer = new HtmlRenderer();

			// Metadata
			var author = context.Series.Metadata?.Authors?.FirstOrDefault();
			var description = ExtractDescription(context.Series.Metadata?.Description);
			var tags = context.Series.Metadata?.Tags?.ToList();
			packageBuilder.SetMetadata(context.Series.Title, author, description : description, tags : tags);

			// CSS
			packageBuilder.AddCss(context.Structure.GlobalCss ?? EpubStylesheet.DEFAULT_CSS);

			// Cover (EPUB-specific: register into package with EPUB paths)
			var coverPath = RegisterCover(context, packageBuilder);

			// Images (EPUB-specific: register into package, build local path map)
			var imageFileMap = RegisterImages(context, packageBuilder);

			// Sections
			await ProcessSections(context, packageBuilder, renderer, coverPath, imageFileMap, author);

			var stream = await packageBuilder.BuildAsync();
			var fileName = $"{ExportUtilities.SanitizeFileName(context.Series.Title)}.epub";

			return ExportResult.Ok(stream, fileName, "application/epub+zip");
		}
		catch (Exception ex) {
			LogFailedToExportSeriesIdToEpub(context.Series.Id, ex);
			return ExportResult.Fail(ex.Message);
		}
	}

	private static string? RegisterCover(BookExportContext context, EpubPackageBuilder packageBuilder) {
		if (context.CoverAsset == null || context.CoverStreamProvider == null) {
			return null;
		}

		var ext = Path.GetExtension(context.CoverAsset.Path)
			.DefaultIfNullOrEmpty(EpubConstants.Defaults.ImageExtension);
		var localPath = $"{EpubConstants.Paths.ImagesFolder}cover{ext}";

		packageBuilder.AddImageFileStream(
			$"{EpubConstants.Paths.OebpsPrefix}{localPath}",
			context.CoverStreamProvider);
		packageBuilder.SetCoverImage(localPath);

		return localPath;
	}

	private static Dictionary<string, string> RegisterImages(
		BookExportContext context,
		EpubPackageBuilder packageBuilder) {
		var imageFileMap = new Dictionary<string, string>();
		var index = 1;

		foreach (var (assetKey, resolved) in context.ImageAssets) {
			var ext = Path.GetExtension(resolved.Asset.Path)
				.DefaultIfNullOrEmpty(EpubConstants.Defaults.ImageExtension);
			var relativePath = $"img{index:D3}{ext}";

			packageBuilder.AddImageFileStream(
				$"{EpubConstants.Paths.OebpsPrefix}{EpubConstants.Paths.ImagesFolder}{relativePath}",
				resolved.StreamProvider);

			imageFileMap[assetKey] = relativePath;
			index++;
		}

		return imageFileMap;
	}

	private async Task ProcessSections(
		BookExportContext context,
		EpubPackageBuilder packageBuilder,
		HtmlRenderer renderer,
		string? coverPath,
		Dictionary<string, string> imageFileMap,
		string? author) {
		var processorContext = new EpubSectionProcessorContext {
			Series = context.Series,
			Volumes = context.Volumes,
			ChapterMap = context.ChapterMap,
			PackageBuilder = packageBuilder,
			Renderer = renderer,
			CoverLocalPath = coverPath,
			ImageFileMap = imageFileMap,
			Author = author
		};

		var tocSectionsToRender = new List<ExportSectionDto>();

		foreach (var section in context.Structure.Sections) {
			var sectionType = section.Type;

			switch (sectionType) {
				case BookSection.Toc or BookSection.TableOfContents:
					processorContext.PackageBuilder.AddNavPoint(new NavPoint {
						Title = EpubConstants.LocalizedText.TableOfContents, ContentSrc = "nav.xhtml"
					});
					tocSectionsToRender.Add(section);
					continue;
				case BookSection.Description: {
					var introSection = context.Structure.Sections.FirstOrDefault(s =>
						s.Type is BookSection.IntroPage or BookSection.Intro);
					var introIncludesDescription = !ExportUtilities.IsSplitDescriptionEnabled(introSection);

					if (introIncludesDescription) {
						LogSkippingSeparateDescriptionSectionIncludedInIntro();
						continue;
					}

					break;
				}
			}

			if (sectionProcessorFactory.GetProcessor(sectionType) is not { } processor) {
				LogUnknownSectionType(section.Type);
				continue;
			}

			await processor.ProcessAsync(section, processorContext);
		}

		foreach (var tocSection in tocSectionsToRender) {
			var navHtml = processorContext.Renderer.RenderToc(processorContext.PackageBuilder.GetNavPoints());

			if (tocSection.CustomCss != null) {
				navHtml = HtmlHelper.InjectCustomCss(navHtml, tocSection.CustomCss);
			}

			processorContext.PackageBuilder.AddHtmlFile(EpubConstants.Paths.NavFile, navHtml);
		}
	}

	private static string? ExtractDescription(List<TextSegmentModel>? segments) =>
		segments == null || segments.Count == 0
			? null
			: string.Join(" ", segments.SelectMany(d => d.Runs.Select(r => r.Text)));

	[LoggerMessage(LogLevel.Error, "Failed to export series {SeriesId} to EPUB")]
	partial void LogFailedToExportSeriesIdToEpub(Guid seriesId, Exception exception);

	[LoggerMessage(LogLevel.Information, "Skipping separate description section (included in intro)")]
	partial void LogSkippingSeparateDescriptionSectionIncludedInIntro();

	[LoggerMessage(LogLevel.Warning, "Unknown section type: {SectionType}")]
	partial void LogUnknownSectionType(BookSection sectionType);
}
