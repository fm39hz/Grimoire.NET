namespace Grimoire.Infrastructure.Export;

using Application.Export;
using Application.Extensions;
using Application.Service.Strategy;
using Common;
using Epub;
using Microsoft.Extensions.Logging;

/// <summary>
///     Strategy for exporting series to EPUB format
/// </summary>
public partial class EpubExportStrategy(
	ILogger<EpubExportStrategy> logger,
	ITemplateEngine templateEngine,
	IEpubPackageBuilderFactory packageBuilderFactory) : IExportStrategy {
	public ExportFormat Format => ExportFormat.Epub;

	public async Task<ExportResult> ExportAsync(BookExportContext context) {
		try {
			var packageBuilder = packageBuilderFactory.Create();

			// Metadata
			var author = context.Series.Metadata?.Authors?.FirstOrDefault();
			packageBuilder.SetMetadata(context.Series.Title, author, description: context.PlainTextDescription,
				tags: context.Series.Metadata?.Tags?.ToList());

			// CSS
			packageBuilder.AddCss(context.Structure.GlobalCss ?? EpubStylesheet.DEFAULT_CSS);

			// Assets
			var coverPath = RegisterCover(context, packageBuilder);
			RegisterImages(context, packageBuilder);

			// Generate all section pages
			await ProcessSections(context, packageBuilder, coverPath, author);

			var stream = await packageBuilder.BuildAsync();
			var fileName = $"{ExportUtilities.SanitizeFileName(context.Series.Title)}.epub";

			return ExportResult.Ok(stream, fileName, "application/epub+zip");
		}
		catch (Exception ex) {
			LogFailedToExportSeriesIdToEpub(context.Series.Id, ex);
			return ExportResult.Fail(ex.Message);
		}
	}

	private async Task ProcessSections(
		BookExportContext context,
		EpubPackageBuilder packageBuilder,
		string? coverPath,
		string? author) {
		var chapterIndex = 1;
		var volumeIndex = 1;

		foreach (var section in context.Structure.Sections) {
			switch (section.Type) {
				case BookSection.IntroPage or BookSection.Intro: {
						var introHtml = await templateEngine.RenderAsync("epub_intro",
							new {
								context.Series.Title,
								Author = author,
								context.Series.Metadata?.Tags,
								context.Series.Metadata?.Description,
								Section = section,
								CoverLocalPath = coverPath,
								ImageFileMap = context.AssetFileMap
							});

						packageBuilder.AddHtmlFile("OEBPS/intro.xhtml", introHtml);
						packageBuilder.AddNavPoint(new NavPoint {
							Title = EpubConstants.LocalizedText.Introduction,
							ContentSrc = "intro.xhtml"
						});
						break;
					}

				case BookSection.Toc or BookSection.TableOfContents: {
						// TOC is unique as it's built last from all NavPoints, but we add the nav point now
						packageBuilder.AddNavPoint(new NavPoint {
							Title = EpubConstants.LocalizedText.TableOfContents,
							ContentSrc = "nav.xhtml"
						});
						break;
					}

				case BookSection.Description: {
						// Separate description page - only render if description should NOT appear in intro page
						var introSection = context.Structure.Sections.FirstOrDefault(s => s.Type == BookSection.IntroPage);
						if (introSection == null || ExportUtilities.IsSplitDescriptionEnabled(introSection)) {
							// Only render separate description page if splitDescription is enabled or there's no intro page
							var descriptionHtml = await templateEngine.RenderAsync("epub_intro",
								new {
									Title = EpubConstants.LocalizedText.Summary,
									context.Series.Metadata?.Description,
									Section = section,
									ImageFileMap = context.AssetFileMap
								});

							packageBuilder.AddHtmlFile("OEBPS/description.xhtml", descriptionHtml);
							packageBuilder.AddNavPoint(new NavPoint {
								Title = EpubConstants.LocalizedText.Summary,
								ContentSrc = "description.xhtml"
							});
						}
						else {
							// Skip separate description section since description is included in intro page
							LogSkippingSeparateDescriptionSectionIncludedInIntro();
						}

						break;
					}

				case BookSection.Content or BookSection.Chapters: {
						foreach (var volume in context.Volumes) {
							// Volume title page
							var volumeFileName = $"volume_{volumeIndex:D3}.xhtml";
							var volumeHtml = await templateEngine.RenderAsync("epub_volume", new {
								volume.Title,
								CoverImagePath = volume.Metadata?.CoverImage != null &&
												context.AssetFileMap.TryGetValue(volume.Metadata.CoverImage,
													out var path)
									? path
									: null,
								volume.Metadata?.PublicationDate,
								volume.Metadata?.Isbn
							});

							packageBuilder.AddHtmlFile($"OEBPS/{volumeFileName}", volumeHtml);
							var volumeNav = new NavPoint {
								Title = volume.Title,
								ContentSrc = volumeFileName,
								Children = []
							};

							// Chapters within volume
							if (context.ChapterMap.TryGetValue(volume.Id, out var chapters)) {
								foreach (var chapter in chapters) {
									var chapterFileName = $"chapter_{chapterIndex:D3}.xhtml";
									var chapterHtml = await templateEngine.RenderAsync("epub_chapter",
										new {
											chapter.Title,
											chapter.ContentData?.Segments,
											chapter.ContentData?.Footnotes,
											ImageFileMap = context.AssetFileMap
										});

									packageBuilder.AddHtmlFile($"OEBPS/{chapterFileName}", chapterHtml);
									volumeNav.Children.Add(new NavPoint {
										Title = chapter.Title,
										ContentSrc = chapterFileName
									});
									chapterIndex++;
								}
							}

							packageBuilder.AddNavPoint(volumeNav);
							volumeIndex++;
						}

						break;
					}

				case BookSection.Unknown:
					break;
				default:
					break;
			}
		}

		// Finalize navigation file (nav.xhtml)
		var navHtml = await templateEngine.RenderAsync("epub_toc", new { NavPoints = packageBuilder.GetNavPoints() });
		packageBuilder.AddHtmlFile(EpubConstants.Paths.NavFile, navHtml);
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

	private static void RegisterImages(
		BookExportContext context,
		EpubPackageBuilder packageBuilder) {
		foreach (var (assetKey, resolved) in context.ImageAssets) {
			var relativePath = context.AssetFileMap[assetKey];

			packageBuilder.AddImageFileStream(
				$"{EpubConstants.Paths.OebpsPrefix}{EpubConstants.Paths.ImagesFolder}{relativePath}",
				resolved.StreamProvider);
		}
	}

	[LoggerMessage(LogLevel.Error, "Failed to export series {SeriesId} to EPUB")]
	partial void LogFailedToExportSeriesIdToEpub(Guid seriesId, Exception exception);

	[LoggerMessage(LogLevel.Information, "Skipping separate description section (included in intro)")]
	partial void LogSkippingSeparateDescriptionSectionIncludedInIntro();

	[LoggerMessage(LogLevel.Warning, "Unknown section type: {SectionType}")]
	partial void LogUnknownSectionType(BookSection sectionType);
}
