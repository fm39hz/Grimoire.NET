namespace Grimoire.Infrastructure;

using Application.Service.Strategy;
using Domain.Common.Repository;
using Export;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Repository;

public static class DependencyInjection {
	public static IServiceCollection AddInfrastructure(this IServiceCollection services) {
		// Register repositories
		services.AddScoped<ISeriesRepository, SeriesRepository>();
		services.AddScoped<IVolumeRepository, VolumeRepository>();
		services.AddScoped<IChapterRepository, ChapterRepository>();
		services.AddScoped<IAssetRepository, AssetRepository>();
		services.AddScoped<ISourceMaterialRepository, SourceMaterialRepository>();

		// Register export strategies
		services.AddScoped<IExportStrategy, EpubExportStrategy>();
		services.AddScoped<IExportStrategy, PdfExportStrategy>();
		services.AddScoped<IExportStrategy, HtmlExportStrategy>();

		return services;
	}
}
