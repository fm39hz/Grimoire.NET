namespace Grimoire.Infrastructure.Export;

using Application.Common;
using Application.Dto.Book;
using Application.Extensions;
using Application.Service.Contract;
using Application.Service.Strategy;
using Common;
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

			// Initialize builders and renderers
			var packageBuilder = new EpubPackageBuilder();
			var renderer = new HtmlRenderer();

			// Set metadata
			var author = series.Metadata?.Authors?.FirstOrDefault();
			var description = series.Metadata?.Description != null && series.Metadata.Description.Count > 0
				? string.Join(" ", series.Metadata.Description.SelectMany(d => d.Runs.Select(r => r.Text)))
				: null;
			packageBuilder.SetMetadata(series.Title, author, description : description);

			// Add CSS
			if (request.Structure?.GlobalCss != null) {
				packageBuilder.AddCss(request.Structure.GlobalCss);
			}
			else {
				packageBuilder.AddCss(EpubStylesheet.DefaultCss);
			}

			// Process cover image if available
			string? coverLocalPath = null;
			if (!string.IsNullOrEmpty(series.Metadata?.CoverImage)) {
				if (PrefixedId.TryToGuid(series.Metadata.CoverImage, EntityPrefix.Asset, out var coverAssetId)) {
					var coverAsset = await assetService.FindOne(coverAssetId);
					if (coverAsset != null) {
						var coverExtension = Path.GetExtension(coverAsset.Path);
						if (string.IsNullOrEmpty(coverExtension)) {
							coverExtension = ".jpg";
						}

						coverLocalPath = $"images/cover{coverExtension}";
						packageBuilder.AddImageFileStream($"OEBPS/{coverLocalPath}",
							async () => await storageService.GetFileStreamAsync(coverAssetId));
						packageBuilder.SetCoverImage(coverLocalPath);
					}
				}
			}

			// Bulk process all images for the entire series upfront
			var imageFileMap = await BulkProcessSeriesImages(series.Id, volumeList, packageBuilder);

			// Process structure sections (always present with default)
			await ProcessStructuredSections(request.Structure.Sections, series, volumeList,
				packageBuilder, renderer, coverLocalPath, imageFileMap, author);

			// Build EPUB package (async to handle streams)
			var epubStream = await packageBuilder.BuildAsync();
			var fileName = $"{ExportUtilities.SanitizeFileName(series.Title)}.epub";

			return new ExportResult {
				ContentStream = epubStream, FileName = fileName, ContentType = "application/epub+zip", Success = true
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

	/// <summary>
	///     Bulk process all images for the entire series at once
	///     Downloads all images and maps them properly with chapter_name/image_name structure
	/// </summary>
	private async Task<Dictionary<string, string>> BulkProcessSeriesImages(
		Guid seriesId,
		List<VolumeModel> volumes,
		EpubPackageBuilder packageBuilder) {
		var imageFileMap = new Dictionary<string, string>(); // AssetKey (assetId) -> relative path in EPUB
		var assetToPath = new Dictionary<Guid, string>();    // Track asset reuse across chapters

		logger.LogInformation("Starting bulk image processing for series: {SeriesId}", seriesId);

		// Get all chapters from all volumes
		var allChapters = new List<(ChapterModel chapter, string chapterName)>();
		foreach (var volume in volumes) {
			var chapters = await volumeService.FindAllChapters(volume.Id);
			var orderedChapters = chapters.OrderBy(c => c.Order).ToList();

			logger.LogInformation("Volume '{VolumeTitle}' has {ChapterCount} chapters", volume.Title, orderedChapters.Count);

			foreach (var chapter in orderedChapters) {
				var chapterWithContent = await chapterService.FindOne(chapter.Id);
				if (chapterWithContent?.ContentData != null) {
					// Sanitize chapter name for filesystem
					var sanitizedChapterName = SanitizeFileName(chapterWithContent.Title);
					allChapters.Add((chapterWithContent, sanitizedChapterName));

					var imageCount = chapterWithContent.ContentData.Segments.OfType<ImageSegmentModel>().Count();
					logger.LogInformation("Chapter '{ChapterTitle}' has {ImageCount} images", chapterWithContent.Title, imageCount);
				}
			}
		}

		logger.LogInformation("Total chapters to process: {ChapterCount}", allChapters.Count);

		// Process each chapter's images
		var totalImages = 0;
		foreach (var (chapter, chapterName) in allChapters) {
			var imageSegments = chapter.ContentData!.Segments
				.OfType<ImageSegmentModel>()
				.ToList();

			// Track image index within this chapter
			var imageIndex = 1;

			foreach (var imageSegment in imageSegments) {
				// Parse AssetKey using PrefixedId utility (format: "ast_guid")
				if (!PrefixedId.TryToGuid(imageSegment.AssetKey, EntityPrefix.Asset, out var assetId)) {
					// Fallback for legacy data or invalid format
					logger.LogWarning("Invalid asset ID format: {AssetKey}", imageSegment.AssetKey);
					imageFileMap[imageSegment.AssetKey] = imageSegment.AssetKey;
					continue;
				}

				logger.LogDebug("Processing image - AssetKey: {AssetKey}, Asset ID: {AssetId}", 
					imageSegment.AssetKey, assetId);

				// Check if this asset was already processed
				if (assetToPath.TryGetValue(assetId, out var existingPath)) {
					// Reuse existing path
					logger.LogDebug("Reusing existing path for asset {AssetId}: {Path}", assetId, existingPath);
					imageFileMap[imageSegment.AssetKey] = existingPath;
					continue;
				}

				// Fetch asset from service
				var asset = await assetService.FindOne(assetId);
				if (asset == null) {
					logger.LogWarning("Asset {AssetId} not found", assetId);
					continue;
				}

				logger.LogDebug("Found asset: {AssetPath}", asset.Path);

				// Create structured filename: chapterName/image_N.ext
				var extension = Path.GetExtension(asset.Path);
				if (string.IsNullOrEmpty(extension)) {
					extension = ".jpg"; // Default fallback
				}

				var imageFileName = $"{chapterName}_img{imageIndex:D3}{extension}";
				var relativePath = $"{chapterName}/{imageFileName}";

				// Add image to EPUB using stream provider (lazy loading)
				var capturedAssetId = assetId; // Capture for closure
				packageBuilder.AddImageFileStream($"OEBPS/images/{relativePath}",
					async () => await storageService.GetFileStreamAsync(capturedAssetId));

				logger.LogDebug("Registered stream for EPUB: OEBPS/images/{RelativePath}", relativePath);

				// Map AssetKey to relative filename for HTML rendering
				imageFileMap[imageSegment.AssetKey] = relativePath;
				assetToPath[assetId] = relativePath;

				totalImages++;
				imageIndex++;
			}
		}

		logger.LogInformation("Bulk image processing complete. Total images processed: {TotalImages}", totalImages);
		logger.LogInformation("ImageFileMap has {EntryCount} entries", imageFileMap.Count);

		return imageFileMap;
	}

	private async Task ProcessStructuredSections(
		List<ExportSectionDto> sections,
		SeriesModel series,
		List<VolumeModel> volumeList,
		EpubPackageBuilder packageBuilder,
		HtmlRenderer renderer,
		string? coverLocalPath,
		Dictionary<string, string> imageFileMap,
		string? author) {
		// Separate sections by dependency
		var independentSections = new List<(ExportSectionDto section, Func<Task> action)>();
		var contentSection = sections.FirstOrDefault(s =>
			s.Type.Equals("content", StringComparison.OrdinalIgnoreCase) ||
			s.Type.Equals("chapters", StringComparison.OrdinalIgnoreCase));
		var tocSection = sections.FirstOrDefault(s =>
			s.Type.Equals("toc", StringComparison.OrdinalIgnoreCase) ||
			s.Type.Equals("tableofcontents", StringComparison.OrdinalIgnoreCase));
		var introSection = sections.FirstOrDefault(s =>
			s.Type.Equals("intropage", StringComparison.OrdinalIgnoreCase) ||
			s.Type.Equals("intro", StringComparison.OrdinalIgnoreCase));
		var descriptionSection = sections.FirstOrDefault(s =>
			s.Type.Equals("description", StringComparison.OrdinalIgnoreCase));

		// Check if IntroPage includes description
		var introIncludesDescription = introSection?.Options?.TryGetValue("splitDescription", out var splitVal) != true
										|| (splitVal is bool split && !split);

		// Collect independent sections that can run in parallel
		foreach (var section in sections) {
			switch (section.Type.ToLowerInvariant()) {
				case "intropage":
				case "intro":
					independentSections.Add((section, async () =>
						await ProcessIntroSection(section, series, packageBuilder, renderer, coverLocalPath, author)));
					break;

				case "description":
					// Only process Description as separate page if not included in IntroPage
					if (!introIncludesDescription) {
						independentSections.Add((section, async () =>
							await ProcessDescriptionSection(section, series, packageBuilder, renderer, author)));
					}

					break;

				case "toc":
				case "tableofcontents":
				case "content":
				case "chapters":
					// These will be processed separately (TOC must be last, Content must be sequential)
					break;

				default:
					logger.LogWarning("Unknown section type: {SectionType}", section.Type);
					break;
			}
		}

		// Run independent sections in parallel
		if (independentSections.Count > 0) {
			await Task.WhenAll(independentSections.Select(s => s.action()));
		}

		// Process content sequentially (chapters need order)
		if (contentSection != null) {
			await ProcessContentSection(contentSection, volumeList, packageBuilder, renderer, imageFileMap);
		}

		// Always add TOC at the end (depends on all NavPoints being added)
		var navHtml = renderer.RenderToc(packageBuilder.GetNavPoints());
		if (tocSection?.CustomCss != null) {
			navHtml = InjectCustomCssIntoHtml(navHtml, tocSection.CustomCss);
		}

		packageBuilder.AddHtmlFile("OEBPS/nav.xhtml", navHtml);
	}

	private async Task ProcessIntroSection(
		ExportSectionDto section,
		SeriesModel series,
		EpubPackageBuilder packageBuilder,
		HtmlRenderer renderer,
		string? coverLocalPath,
		string? author) {
		var intro = new IntroViewModel {
			BookTitle = series.Title,
			Author = author,
			Tags = series.Metadata?.Tags?.ToList(),
			Description = ExportUtilities.IsSplitDescriptionEnabled(section)? null : series.Metadata?.Description,
			CoverLocalPath = coverLocalPath
		};

		var introHtml = renderer.RenderIntro(intro);
		if (section.CustomCss != null) {
			introHtml = InjectCustomCssIntoHtml(introHtml, section.CustomCss);
		}

		packageBuilder.AddHtmlFile("OEBPS/intro.xhtml", introHtml);
		packageBuilder.AddNavPoint(new NavPoint { Title = "Giới thiệu", ContentSrc = "intro.xhtml" });

		await Task.CompletedTask;
	}

	private async Task ProcessDescriptionSection(
		ExportSectionDto section,
		SeriesModel series,
		EpubPackageBuilder packageBuilder,
		HtmlRenderer renderer,
		string? author) {
		if (series.Metadata?.Description == null || series.Metadata.Description.Count == 0) {
			return;
		}

		var intro = new IntroViewModel {
			BookTitle = series.Title,
			Author = author,
			Tags = null,
			Description = series.Metadata.Description,
			CoverLocalPath = null
		};

		var descHtml = renderer.RenderIntro(intro);
		if (section.CustomCss != null) {
			descHtml = InjectCustomCssIntoHtml(descHtml, section.CustomCss);
		}

		packageBuilder.AddHtmlFile("OEBPS/description.xhtml", descHtml);
		packageBuilder.AddNavPoint(new NavPoint { Title = "Tóm tắt", ContentSrc = "description.xhtml" });

		await Task.CompletedTask;
	}

	private async Task ProcessContentSection(
		ExportSectionDto section,
		List<VolumeModel> volumeList,
		EpubPackageBuilder packageBuilder,
		HtmlRenderer renderer,
		Dictionary<string, string> imageFileMap) {
		var chapterIndex = 1;
		foreach (var volume in volumeList) {
			var chapters = await volumeService.FindAllChapters(volume.Id);
			var orderedChapters = chapters.OrderBy(c => c.Order).ToList();

			var volumeNav = new NavPoint {
				Title = volume.Title, ContentSrc = $"chapter_{chapterIndex}.xhtml", Children = []
			};

			foreach (var chapter in orderedChapters) {
				var chapterWithContent = await chapterService.FindOne(chapter.Id);
				if (chapterWithContent?.ContentData == null) {
					continue;
				}

				var chapterFileName = $"chapter_{chapterIndex}.xhtml";

				var chapterViewModel = new ChapterViewModel {
					Title = chapterWithContent.Title,
					Segments = chapterWithContent.ContentData.Segments,
					Footnotes = chapterWithContent.ContentData.Footnotes,
					FileName = chapterFileName,
					ImageFileMap = imageFileMap
				};

				var chapterHtml = renderer.RenderChapter(chapterViewModel);
				if (section.CustomCss != null) {
					chapterHtml = InjectCustomCssIntoHtml(chapterHtml, section.CustomCss);
				}

				packageBuilder.AddHtmlFile($"OEBPS/{chapterFileName}", chapterHtml);

				volumeNav.Children.Add(new NavPoint { Title = chapterWithContent.Title, ContentSrc = chapterFileName });

				chapterIndex++;
			}

			if (volumeNav.Children.Count > 0) {
				packageBuilder.AddNavPoint(volumeNav);
			}
		}
	}

	private static string InjectCustomCssIntoHtml(string html, string customCss) {
		var headCloseIndex = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
		if (headCloseIndex > 0) {
			var styleTag = $"\n<style>\n{customCss}\n</style>\n";
			return html.Insert(headCloseIndex, styleTag);
		}

		return html;
	}

	private static string SanitizeFileName(string fileName) =>
		ExportUtilities.SanitizeFileName(fileName.FoldToASCII().Replace(": ", "_").Replace(" ", "_"));
}
