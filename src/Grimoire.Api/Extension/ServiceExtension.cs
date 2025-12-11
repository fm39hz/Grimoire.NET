namespace Grimoire.Api.Extension;

using Application;
using Infrastructure;
using JetBrains.Annotations;

public static class ServiceExtension {
	[UsedImplicitly]
	public static IServiceCollection AddServices(this IServiceCollection service, WebApplicationBuilder builder) {
		service.AddApplication();
		service.AddInfrastructure(builder.Configuration);

		return service;
	}
}
