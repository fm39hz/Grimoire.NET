namespace Grimoire.Api.Extension;

using Application.Service.Contract;
using Application.Service.Implementation;
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

		service.AddScoped<ISeriesService, SeriesService>();
		service.AddScoped<IVolumeService, VolumeService>();
		service.AddScoped<IChapterService, ChapterService>();


		return service;
	}
}
