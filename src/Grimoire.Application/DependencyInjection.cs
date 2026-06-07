namespace Grimoire.Application;

using Export;
using Import;
using Mapper;
using Microsoft.Extensions.DependencyInjection;
using Service.Contract;
using Service.Implementation;
using Service.Strategy;

public static class DependencyInjection {
	public static IServiceCollection AddApplication(this IServiceCollection services) {
		// Register services
		services.AddScoped<ISeriesService, SeriesService>();
		services.AddScoped<IVolumeService, VolumeService>();
		services.AddScoped<IChapterService, ChapterService>();
		services.AddScoped<IBinderyService, BinderyService>();
		services.AddScoped<IAssetService, AssetService>();
		services.AddScoped<IAssetOwnershipService, AssetOwnershipService>();
		services.AddScoped<ISeriesSyncService, SeriesSyncService>();
		services.AddScoped<IStorageService, StorageService>();
		services.AddScoped<IBookTreeService, BookTreeService>();

		// Register mappers
		services.AddScoped<IBookMapper, BookMapper>();

		// Register export collaborators
		services.AddScoped<VolumeResolver>();
		services.AddScoped<ChapterLoader>();
		services.AddScoped<CoverResolver>();
		services.AddScoped<ImageAssetCollector>();
		services.AddScoped<BookExportOrchestrator>();

		// Register ingestion strategies in priority order
		services.AddScoped<IIngestionStrategy, PreProcessedIngestionStrategy>();
		services.AddScoped<IIngestionStrategy, RawMarkdownIngestionStrategy>();
		services.AddScoped<IngestionStrategyFactory>();

		// Register import strategies
		services.AddScoped<IImportStrategy, EpubImportStrategy>();
		services.AddScoped<ImportStrategyFactory>();

		// Register import orchestrator
		services.AddScoped<IImportOrchestrator, ImportOrchestrator>();

		// Register import collaborators
		services.AddScoped<IVolumeTreeResolver, VolumeTreeResolver>();
		services.AddScoped<IChapterImportHandler, ChapterImportHandler>();
		services.AddScoped<IMediaImportService, MediaImportService>();

		return services;
	}
}
