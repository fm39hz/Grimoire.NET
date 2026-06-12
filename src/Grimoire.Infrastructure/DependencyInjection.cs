namespace Grimoire.Infrastructure;

using Application.Export;
using Application.Import;
using Application.Service.Strategy;
using Configuration;
using Domain.Common.Repository;
using Export;
using Export.Common;
using Export.Epub;
using Export.Markdown;
using Import;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Repository;

public static class DependencyInjection {
	[UsedImplicitly]
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) {
		// Register Unit of Work
		services.AddScoped<IUnitOfWork, UnitOfWork>();

		// Register repositories
		services.AddScoped<IBookTreeRepository, BookTreeRepository>();
		services.AddScoped<ISeriesRepository, SeriesRepository>();
		services.AddScoped<IVolumeRepository, VolumeRepository>();
		services.AddScoped<IChapterRepository, ChapterRepository>();
		services.AddScoped<IAssetRepository, AssetRepository>();
		services.AddScoped<ISourceMaterialRepository, SourceMaterialRepository>();
		services.AddScoped<ISeriesExportRecordRepository, SeriesExportRecordRepository>();

		// Register storage configuration
		var storageSection = configuration.GetSection(StorageConfiguration.SECTION_NAME);
		var storageConfig = new StorageConfiguration();
		storageSection.Bind(storageConfig);

		// Register storage repository based on configuration
		var storageType = storageConfig.Type;
		switch (storageType) {
			case "S3":
				services.Configure<S3Configuration>(storageSection.GetRequiredSection("S3"));
				services.AddScoped<IStorageRepository, S3StorageRepository>();
				break;
			case "LocalStorage":
				services.AddScoped<IStorageRepository, LocalStorageRepository>();
				break;
			default:
				throw new InvalidOperationException($"Unknown storage type: {storageType}");
		}

		// Template engine
		services.AddSingleton<ITemplateEngine, ScribanTemplateEngine>();

		// Package builders
		services.AddTransient<IPackageBuilderFactory, PackageBuilderFactory>();
		services.AddScoped<IPackageBuilder, EpubPackageBuilder>();

		// Export strategies
		services.AddScoped<ISectionRendererFactory, SectionRendererFactory>();

		services.AddScoped<ISectionRenderer, EpubSectionRenderer>();
		services.AddScoped<ISectionRenderer, MarkdownSectionRenderer>();
		services.AddScoped<IExportStrategy, EpubExportStrategy>();
		services.AddScoped<IExportStrategy, MarkdownExportStrategy>();

		// Import infrastructure
		services.AddScoped<IInlineTagHandler, BrTagHandler>();
		services.AddScoped<IInlineTagHandler, ImgTagHandler>();
		services.AddScoped<IInlineTagHandler, EmphasisTagHandler>();
		services.AddScoped<IInlineTagHandler, StrongTagHandler>();
		services.AddScoped<IInlineTagHandler, AnchorTagHandler>();
		services.AddScoped<IInlineTagHandler, SpanTagHandler>();
		services.AddScoped<IEpubParser, EpubParser>();
		services.AddScoped<IXhtmlSegmentParser, XhtmlSegmentParser>();

		return services;
	}
}
