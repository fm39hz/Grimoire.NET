namespace Grimoire.Infrastructure;

using Application.Export;
using Application.Service.Strategy;
using Configuration;
using Domain.Common.Repository;
using Export;
using Grimoire.Infrastructure.Export.Common;
using Grimoire.Infrastructure.Export.Epub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Repository;

public static class DependencyInjection {
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) {
		// Register Unit of Work
		services.AddScoped<IUnitOfWork, UnitOfWork>();

		// Register repositories
		services.AddScoped<ISeriesRepository, SeriesRepository>();
		services.AddScoped<IVolumeRepository, VolumeRepository>();
		services.AddScoped<IChapterRepository, ChapterRepository>();
		services.AddScoped<IAssetRepository, AssetRepository>();
		services.AddScoped<ISourceMaterialRepository, SourceMaterialRepository>();

		// Register storage configuration
		var storageSection = configuration.GetSection(StorageConfiguration.SECTION_NAME);
		var storageConfig = new StorageConfiguration();
		storageSection.Bind(storageConfig);

		// Register storage repository based on configuration
		var storageType = storageConfig.Type;
		switch (storageType) {
			case "S3":
				// services.AddScoped<IStorageRepository, S3StorageRepository>();
				throw new NotImplementedException("S3 storage is not implemented yet.");
			case "LocalStorage":
				services.AddScoped<IStorageRepository, LocalStorageRepository>();
				break;
			default:
				throw new InvalidOperationException($"Unknown storage type: {storageType}");
		}

		// Register export strategies
		services.AddScoped<IExportStrategy, EpubExportStrategy>();
		services.AddScoped<IExportStrategy, PdfExportStrategy>();
		services.AddScoped<IExportStrategy, HtmlExportStrategy>();
		services.AddScoped<IExportStrategy, MarkdownExportStrategy>();

		// Register section processor factories
		services.AddScoped<ISectionProcessorFactory<EpubSectionProcessorContext>>(sp =>
			new SectionProcessorFactory<EpubSectionProcessorContext>(sp, ExportFormat.Epub));

		// Register EPUB processors with keyed services
		services.AddKeyedScoped<ISectionProcessor<EpubSectionProcessorContext>, IntroSectionProcessor>($"{ExportFormat.Epub}:{BookSection.Intro.ToString().ToLowerInvariant()}");
		services.AddKeyedScoped<ISectionProcessor<EpubSectionProcessorContext>, IntroSectionProcessor>($"{ExportFormat.Epub}:{BookSection.IntroPage.ToString().ToLowerInvariant()}");
		services.AddKeyedScoped<ISectionProcessor<EpubSectionProcessorContext>, DescriptionSectionProcessor>($"{ExportFormat.Epub}:{BookSection.Description.ToString().ToLowerInvariant()}");
		services.AddKeyedScoped<ISectionProcessor<EpubSectionProcessorContext>, ContentSectionProcessor>($"{ExportFormat.Epub}:{BookSection.Content.ToString().ToLowerInvariant()}");
		services.AddKeyedScoped<ISectionProcessor<EpubSectionProcessorContext>, ContentSectionProcessor>($"{ExportFormat.Epub}:{BookSection.Chapters.ToString().ToLowerInvariant()}");
		services.AddKeyedScoped<ISectionProcessor<EpubSectionProcessorContext>, TocSectionProcessor>($"{ExportFormat.Epub}:{BookSection.Toc.ToString().ToLowerInvariant()}");
		services.AddKeyedScoped<ISectionProcessor<EpubSectionProcessorContext>, TocSectionProcessor>($"{ExportFormat.Epub}:{BookSection.TableOfContents.ToString().ToLowerInvariant()}");

		return services;
	}
}
