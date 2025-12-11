namespace Grimoire.Infrastructure;

using Application.Service.Strategy;
using Configuration;
using Domain.Common.Repository;
using Export;
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
		var storageSection = configuration.GetSection(StorageConfiguration.SectionName);
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

		return services;
	}
}
