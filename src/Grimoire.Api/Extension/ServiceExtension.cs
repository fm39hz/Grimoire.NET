namespace Grimoire.Api.Extension;

using Application.Mapper;
using Application.Service.Contract;
using Application.Service.Implementation;
using Application.Service.Strategy;
using Domain.Common.Repository;
using Infrastructure.Persistence.Repository;
using JetBrains.Annotations;

public static class ServiceExtension {
	[UsedImplicitly]
	public static IServiceCollection AddServices(this IServiceCollection service) {
		service.AddScoped<ISeriesRepository, SeriesRepository>();
		service.AddScoped<IVolumeRepository, VolumeRepository>();
		service.AddScoped<IChapterRepository, ChapterRepository>();
		service.AddScoped<IAssetRepository, AssetRepository>();
		service.AddScoped<ISourceMaterialRepository, SourceMaterialRepository>();

		service.AddScoped<ISeriesService, SeriesService>();
		service.AddScoped<IVolumeService, VolumeService>();
		service.AddScoped<IChapterService, ChapterService>();

		service.AddScoped<IBookMapper, BookMapper>();

		// Register ingestion strategies in priority order
		service.AddScoped<IIngestionStrategy, PreProcessedIngestionStrategy>();
		service.AddScoped<IIngestionStrategy, RawMarkdownIngestionStrategy>();
		service.AddScoped<IngestionStrategyFactory>();

		return service;
	}
}
