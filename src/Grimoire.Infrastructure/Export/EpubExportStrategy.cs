namespace Grimoire.Infrastructure.Export;

using Application.Dto.Book;
using Application.Extensions;
using Application.Service.Contract;
using Application.Service.Strategy;
using Common;
using Domain.Common;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Epub;
using Microsoft.Extensions.Logging;

/// <summary>
///     Strategy for exporting series to EPUB format
/// </summary>
public class EpubExportStrategy(
	IChapterService chapterService,
	IVolumeService volumeService,
	IStorageService storageService,
	IAssetService assetService,
	ILogger<EpubExportStrategy> logger) : IExportStrategy {

	private readonly ImageAssetProcessor _imageProcessor = new(assetService, storageService, logger);

	public ExportFormat Format => ExportFormat.Epub;

	public async Task<ExportResult> ExportAsync(
		SeriesModel series,
		IEnumerable<VolumeModel> volumes,
		BinderyRequestDto request) {
		try {
			var volumeList = volumes.OrderBy(v => v.Order).ToList();

			// Initialize core components
			var packageBuilder = new EpubPackageBuilder();
			var renderer = new HtmlRenderer();

			// Setup metadata
			var author = series.Metadata?.Authors?.FirstOrDefault();
			var description = ExtractDescription(series.Metadata?.Description);
			packageBuilder.SetMetadata(series.Title, author, description: description);

			// Add CSS
			AddStylesheet(packageBuilder, request.Structure?.GlobalCss);

			// Process cover image
			var coverLocalPath = await ProcessCoverImage(series, packageBuilder);

			// Bulk process all images for the series
			var imageFileMap = await ProcessAllImages(series.Id, volumeList, packageBuilder);

			// Process sections
			await ProcessSections(
				request.Structure.Sections,
				series,
				volumeList,
				packageBuilder,
				renderer,
				coverLocalPath,
				imageFileMap,
				author);

			// Build and return EPUB
			var epubStream = await packageBuilder.BuildAsync();
			var fileName = $"{ExportUtilities.SanitizeFileName(series.Title)}.epub";

			return new ExportResult {
				ContentStream = epubStream,
				FileName = fileName,
				ContentType = "application/epub+zip",
				Success = true
			};
		}
		catch (Exception ex) {
			logger.LogError(ex, "Failed to export series {SeriesId} to EPUB", series.Id);
			return new ExportResult {
				ContentStream = Stream.Null,
				FileName = string.Empty,
				ContentType = string.Empty,
				Success = false,
				ErrorMessage = ex.Message
			};
		}
	}

	private static string? ExtractDescription(List<TextSegmentModel>? descriptionSegments) =>
		descriptionSegments == null || descriptionSegments.Count == 0
			? null
			: string.Join(" ", descriptionSegments.SelectMany(d => d.Runs.Select(r => r.Text)));

	private static void AddStylesheet(EpubPackageBuilder packageBuilder, string? globalCss) => packageBuilder.AddCss(globalCss ?? EpubStylesheet.DefaultCss);

	private async Task<string?> ProcessCoverImage(SeriesModel series, EpubPackageBuilder packageBuilder) {
		if (string.IsNullOrEmpty(series.Metadata?.CoverImage)) {
			return null;
		}

		if (!PrefixedId.TryToGuid(series.Metadata.CoverImage, EntityPrefix.Asset, out var coverAssetId)) {
			return null;
		}

		var coverAsset = await assetService.FindOne(coverAssetId);
		if (coverAsset == null) {
			return null;
		}

		var coverExtension = Path.GetExtension(coverAsset.Path);
		if (string.IsNullOrEmpty(coverExtension)) {
			coverExtension = EpubConstants.Defaults.ImageExtension;
		}

		var coverLocalPath = $"{EpubConstants.Paths.ImagesFolder}cover{coverExtension}";
		packageBuilder.AddImageFileStream(
			$"{EpubConstants.Paths.OebpsPrefix}{coverLocalPath}",
			async () => await storageService.GetFileStreamAsync(coverAssetId));
		packageBuilder.SetCoverImage(coverLocalPath);

		return coverLocalPath;
	}

	private async Task<Dictionary<string, string>> ProcessAllImages(
		Guid seriesId,
		List<VolumeModel> volumes,
		EpubPackageBuilder packageBuilder) {

		logger.LogInformation("Starting bulk image processing for series: {SeriesId}", seriesId);

		// Collect all chapters from all volumes
		var allChapters = new List<(ChapterModel chapter, string chapterName)>();

		foreach (var volume in volumes) {
			var chapters = await volumeService.FindAllChapters(volume.Id);
			var orderedChapters = chapters.OrderBy(c => c.Order).ToList();

			foreach (var chapter in orderedChapters) {
				var chapterWithContent = await chapterService.FindOne(chapter.Id);
				if (chapterWithContent?.ContentData != null) {
					var sanitizedChapterName = SanitizeFileName(chapterWithContent.Title);
					allChapters.Add((chapterWithContent, sanitizedChapterName));
				}
			}
		}

		logger.LogInformation("Processing images for {ChapterCount} chapters", allChapters.Count);

		// Process images for all chapters
		var imageFileMap = new Dictionary<string, string>();
		var assetToPath = new Dictionary<Guid, string>();
		var totalImages = 0;

		foreach (var (chapter, chapterName) in allChapters) {
			var imageSegments = chapter.ContentData!.Segments
				.OfType<ImageSegmentModel>()
				.ToList();

			var imageIndex = 1;

			foreach (var imageSegment in imageSegments) {
				if (!PrefixedId.TryToGuid(imageSegment.AssetKey, EntityPrefix.Asset, out var assetId)) {
					logger.LogWarning("Invalid asset ID format: {AssetKey}", imageSegment.AssetKey);
					imageFileMap[imageSegment.AssetKey] = imageSegment.AssetKey;
					continue;
				}

				// Reuse existing path if asset was already processed
				if (assetToPath.TryGetValue(assetId, out var existingPath)) {
					imageFileMap[imageSegment.AssetKey] = existingPath;
					continue;
				}

				var asset = await assetService.FindOne(assetId);
				if (asset == null) {
					logger.LogWarning("Asset {AssetId} not found", assetId);
					continue;
				}

				var extension = Path.GetExtension(asset.Path);
				if (string.IsNullOrEmpty(extension)) {
					extension = EpubConstants.Defaults.ImageExtension;
				}

				var imageFileName = $"{chapterName}_img{imageIndex:D3}{extension}";
				var relativePath = $"{chapterName}/{imageFileName}";

				// Add image stream provider (lazy loading)
				var capturedAssetId = assetId;
				packageBuilder.AddImageFileStream(
					$"{EpubConstants.Paths.OebpsPrefix}{EpubConstants.Paths.ImagesFolder}{relativePath}",
					async () => await storageService.GetFileStreamAsync(capturedAssetId));

				imageFileMap[imageSegment.AssetKey] = relativePath;
				assetToPath[assetId] = relativePath;

				totalImages++;
				imageIndex++;
			}
		}

		logger.LogInformation("Bulk image processing complete. Total images: {TotalImages}", totalImages);

		return imageFileMap;
	}

	private async Task ProcessSections(
		List<ExportSectionDto> sections,
		SeriesModel series,
		List<VolumeModel> volumes,
		EpubPackageBuilder packageBuilder,
		HtmlRenderer renderer,
		string? coverLocalPath,
		Dictionary<string, string> imageFileMap,
		string? author) {

		var context = new SectionProcessorContext {
			Series = series,
			Volumes = volumes,
			PackageBuilder = packageBuilder,
			Renderer = renderer,
			CoverLocalPath = coverLocalPath,
			ImageFileMap = imageFileMap,
			Author = author
		};

		// Create section processors
		var processors = new Dictionary<string, ISectionProcessor> {
			[EpubConstants.SectionTypes.IntroPage] = new IntroSectionProcessor(),
			[EpubConstants.SectionTypes.Intro] = new IntroSectionProcessor(),
			[EpubConstants.SectionTypes.Description] = new DescriptionSectionProcessor(),
			[EpubConstants.SectionTypes.Content] = new ContentSectionProcessor(chapterService, volumeService),
			[EpubConstants.SectionTypes.Chapters] = new ContentSectionProcessor(chapterService, volumeService),
			[EpubConstants.SectionTypes.Toc] = new TocSectionProcessor(),
			[EpubConstants.SectionTypes.TableOfContents] = new TocSectionProcessor()
		};

		// Two-pass approach to honor user-defined section order while handling TOC dependency
		
		// First pass: Process all sections in user-defined order
		// TOC sections add placeholder NavPoints but defer HTML rendering until all NavPoints collected
		var tocSectionsToRender = new List<ExportSectionDto>();
		
		for (var i = 0; i < sections.Count; i++) {
			var section = sections[i];
			var sectionType = section.Type.ToLowerInvariant();

			// Check if this is a TOC section (add placeholder NavPoint, defer rendering)
			if (IsSectionType(section, EpubConstants.SectionTypes.Toc, EpubConstants.SectionTypes.TableOfContents)) {
				// Add placeholder NavPoint to mark TOC position in reading order
				context.PackageBuilder.AddNavPoint(new NavPoint {
					Title = EpubConstants.LocalizedText.TableOfContents,
					ContentSrc = "nav.xhtml"
				});
				tocSectionsToRender.Add(section);
				continue;
			}

			// Check if intro includes description (to avoid duplicate description section)
			if (IsSectionType(section, EpubConstants.SectionTypes.Description)) {
				var introSection = sections.FirstOrDefault(s => 
					IsSectionType(s, EpubConstants.SectionTypes.IntroPage, EpubConstants.SectionTypes.Intro));
				var introIncludesDescription = !ExportUtilities.IsSplitDescriptionEnabled(introSection);
				
				if (introIncludesDescription) {
					logger.LogInformation("Skipping separate description section (included in intro)");
					continue;
				}
			}

			// Get processor for this section type
			if (!processors.TryGetValue(sectionType, out var processor)) {
				logger.LogWarning("Unknown section type: {SectionType}", section.Type);
				continue;
			}

			// Process section
			await processor.ProcessAsync(section, context);
		}

		// Second pass: Render TOC HTML now that all NavPoints are collected
		foreach (var tocSection in tocSectionsToRender) {
			var navHtml = context.Renderer.RenderToc(context.PackageBuilder.GetNavPoints());
			
			if (tocSection.CustomCss != null) {
				navHtml = HtmlHelper.InjectCustomCss(navHtml, tocSection.CustomCss);
			}
			
			context.PackageBuilder.AddHtmlFile(EpubConstants.Paths.NavFile, navHtml);
		}
	}

	private static bool IsSectionType(ExportSectionDto section, params string[] types) {
		var sectionType = section.Type.ToLowerInvariant();
		return types.Any(t => t.Equals(sectionType, StringComparison.OrdinalIgnoreCase));
	}

	private static string SanitizeFileName(string fileName) =>
		ExportUtilities.SanitizeFileName(fileName.FoldToASCII().Replace(": ", "_").Replace(" ", "_"));
}
