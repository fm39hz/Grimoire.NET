namespace Grimoire.Infrastructure.Export;

using Application.Export;
using Application.Service.Strategy;
using Common;
using Epub;
using Application.Extensions;
using Domain.Entity.Book;
using Microsoft.Extensions.Logging;

public partial class EpubExportStrategy(
	ILogger<EpubExportStrategy> logger,
	IPackageBuilderFactory packageBuilderFactory,
	ISectionRendererFactory sectionRendererFactory) : IExportStrategy {
	public ExportFormat Format => ExportFormat.Epub;

	public async Task<ExportResult> ExportAsync(BookExportContext context) {
		try {
			var builder = packageBuilderFactory.Create();
			var renderer = sectionRendererFactory.Resolve(Format);

			if (renderer == null) {
				return ExportResult.Fail($"No renderer found for {Format}");
			}

			// 1. Metadata
			builder.SetMetadata(new BookPackageMetadata(
				context.Series.Title,
				context.Series.Metadata?.Authors?.FirstOrDefault(),
				PlainTextDescription: context.PlainTextDescription
			));

			// 2. Global CSS
			builder.AddStylesheet(context.Structure.GlobalCss ?? EpubStylesheet.DEFAULT_CSS);

			// 3. Static Assets
			RegisterAssets(context, builder);

			// 4. Content & Navigation
			var navEntries = new List<NavEntry>();
			foreach (var section in context.Structure.Sections) {
				navEntries.AddRange(renderer.RenderSection(context, section, builder));
			}
			builder.SetNavigation(navEntries);

			// 5. Build
			var stream = await builder.BuildAsync();
			var fileName = $"{ExportUtilities.SanitizeFileName(context.Series.Title)}.epub";

			return ExportResult.Ok(stream, fileName, "application/epub+zip");
		}
		catch (Exception ex) {
			logger.LogError(ex, "Failed to export series {Id}", context.Series.Id);
			return ExportResult.Fail(ex.Message);
		}
	}

	private void RegisterAssets(BookExportContext context, IPackageBuilder builder) {
		// Cover
		if (context.CoverAsset != null && context.CoverStreamProvider != null) {
			var ext = Path.GetExtension(context.CoverAsset.Path).DefaultIfNullOrEmpty(".jpg");
			builder.AddAsset($"cover{ext}", context.CoverStreamProvider, AssetRefType.Cover);
		}

		// Inline Images
		foreach (var (key, asset) in context.ImageAssets) {
			builder.AddAsset(context.AssetFileMap[key], asset.StreamProvider, AssetRefType.Content);
		}
	}
}
