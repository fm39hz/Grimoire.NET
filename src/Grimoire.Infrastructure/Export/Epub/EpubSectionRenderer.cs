namespace Grimoire.Infrastructure.Export.Epub;

using Application.Dto.Book;
using Application.Export;
using Application.Extensions;
using Application.Service.Strategy;
using Common;
using Microsoft.Extensions.Logging;

public partial class EpubSectionRenderer(
	ITemplateEngine templateEngine,
	ILogger<EpubSectionRenderer> logger) : ISectionRenderer {
	public ExportFormat Format => ExportFormat.Epub;



	public IReadOnlyList<NavEntry> RenderSection(
		BookExportContext context,
		ExportSectionDto section,
		IPackageBuilder builder) => section.Type switch {
			BookSection.Intro or BookSection.IntroPage => RenderIntro(context, section, builder),
			BookSection.Toc or BookSection.TableOfContents => RenderToc(builder),
			BookSection.Description => RenderDescription(context, section, builder),
			BookSection.Content or BookSection.Chapters => RenderContent(context, builder),
			BookSection.Unknown or _ => []
		};

	private IReadOnlyList<NavEntry> RenderIntro(BookExportContext context, ExportSectionDto section, IPackageBuilder builder) {
		var html = templateEngine.Render("epub_intro", new {
			context.Series.Title,
			Author = context.Series.Metadata?.Authors?.FirstOrDefault(),
			context.Series.Metadata?.Tags,
			context.Series.Metadata?.Description,
			Section = section,
			CoverLocalPath = ResolveCoverLocalPath(context),
			ImageFileMap = context.AssetFileMap
		});

		builder.AddPage("intro", html, PageRole.Intro);
		return [new NavEntry("intro", EpubConstants.LocalizedText.INTRODUCTION)];
	}

	private static IReadOnlyList<NavEntry> RenderToc(IPackageBuilder builder) {
		// Content is empty because IPackageBuilder generates the nav.xhtml automatically
		builder.AddPage("toc", string.Empty, PageRole.TableOfContents);
		return [new NavEntry("toc", EpubConstants.LocalizedText.TABLE_OF_CONTENTS)];
	}

	private IReadOnlyList<NavEntry> RenderDescription(BookExportContext context, ExportSectionDto section, IPackageBuilder builder) {
		var introSection = context.Structure.Sections.FirstOrDefault(s => s.Type == BookSection.IntroPage);

		if (introSection != null && !ExportUtilities.IsSplitDescriptionEnabled(introSection)) {
			LogSkippingDescription();
			return [];
		}

		var html = templateEngine.Render("epub_intro", new {
			Title = EpubConstants.LocalizedText.SUMMARY,
			context.Series.Metadata?.Description,
			Section = section,
			ImageFileMap = context.AssetFileMap
		});

		builder.AddPage("description", html, PageRole.Description);
		return [new NavEntry("description", EpubConstants.LocalizedText.SUMMARY)];
	}

	private IReadOnlyList<NavEntry> RenderContent(BookExportContext context, IPackageBuilder builder) {
		var navEntries = new List<NavEntry>();

		foreach (var volume in context.Volumes) {
			var volId = $"vol_{volume.Id:N}"[..Math.Min(8, $"vol_{volume.Id:N}".Length)]; // Use volume ID to ensure unique ID
			var volHtml = templateEngine.Render("epub_volume", new {
				volume.Title,
				CoverImagePath = volume.Metadata?.CoverImage != null && context.AssetFileMap.TryGetValue(volume.Metadata.CoverImage, out var path) ? path : null,
				volume.Metadata?.PublicationDate,
				volume.Metadata?.Isbn
			});

			builder.AddPage(volId, volHtml, PageRole.VolumeTitle);

			var children = new List<NavEntry>();
			if (context.ChapterMap.TryGetValue(volume.Id, out var chapters)) {
				foreach (var chapter in chapters) {
					var chId = chapter.Id.ToString();
					var chHtml = templateEngine.Render("epub_chapter", new {
						chapter.Title,
						chapter.ContentData?.Segments,
						chapter.ContentData?.Footnotes,
						ImageFileMap = context.AssetFileMap
					});

					builder.AddPage(chId, chHtml, PageRole.Chapter);
					children.Add(new NavEntry(chId, chapter.Title));
				}
			}

			navEntries.Add(new NavEntry(volId, volume.Title, children));
		}

		return navEntries;
	}

	private static string? ResolveCoverLocalPath(BookExportContext context) {
		if (context.CoverAsset == null) {
			return null;
		}

		var ext = Path.GetExtension(context.CoverAsset.Path).DefaultIfNullOrEmpty(".jpg");
		return $"{EpubConstants.Paths.IMAGES_FOLDER}cover{ext}";
	}

	[LoggerMessage(LogLevel.Information, "Skipping separate description (already in intro)")]
	partial void LogSkippingDescription();
}
