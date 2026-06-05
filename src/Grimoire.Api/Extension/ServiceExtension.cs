namespace Grimoire.Api.Extension;

using Application;
using Grimoire.Job.Jobs;
using Infrastructure;
using JetBrains.Annotations;

public static class ServiceExtension {
	[UsedImplicitly]
	public static IServiceCollection AddServices(this IServiceCollection service, WebApplicationBuilder builder) {
		service.AddApplication();
		service.AddInfrastructure(builder.Configuration);
		
		// Register Hangfire jobs so in-process server can resolve them
		service.AddTransient<ExportJob>();

		return service;
	}
}
