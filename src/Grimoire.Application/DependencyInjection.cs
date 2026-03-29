namespace Grimoire.Application;

using Export;
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
		services.AddScoped<IStorageService, StorageService>();

		// Register mappers
		services.AddScoped<IBookMapper, BookMapper>();

		// Register export orchestrator
		services.AddScoped<BookExportOrchestrator>();

		// Register ingestion strategies in priority order
		services.AddScoped<IIngestionStrategy, PreProcessedIngestionStrategy>();
		services.AddScoped<IIngestionStrategy, RawMarkdownIngestionStrategy>();
		services.AddScoped<IngestionStrategyFactory>();

		return services;
	}
}
