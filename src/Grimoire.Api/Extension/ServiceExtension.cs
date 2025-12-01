namespace Grimoire.Api.Extension;

using Application.Service.Contract;
using Application.Service.Implementation;
using Domain.Common.Repository;
using Infrastructure.Persistence.Repository;
using JetBrains.Annotations;

public static class ServiceExtension {
	[UsedImplicitly]
	public static IServiceCollection AddServices(this IServiceCollection service) {
		// Register repositories
		service.AddScoped<ISeriesRepository, SeriesRepository>();

		// Register services
		service.AddScoped<ISeriesService, SeriesService>();

		return service;
	}
}
