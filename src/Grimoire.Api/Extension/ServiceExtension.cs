namespace Grimoire.Api.Extension;

using Application;
using Application.Publish;
using Grimoire.Api.Publish;
using Grimoire.Infrastructure.Publish;
using Grimoire.Job.Jobs;
using Infrastructure;
using JetBrains.Annotations;

public static class ServiceExtension {
	[UsedImplicitly]
	public static IServiceCollection AddServices(this IServiceCollection service, WebApplicationBuilder builder) {
		service.AddApplication();
		service.AddInfrastructure(builder.Configuration);
		
		// Register IPublishService
		service.AddScoped<IPublishService, PublishService>();
		service.AddSingleton<JobProgressBus>();
		service.AddSingleton<IJobProgressTracker>(sp => sp.GetRequiredService<JobProgressBus>());
		service.AddSingleton<IJobProgressSubscription>(sp => sp.GetRequiredService<JobProgressBus>());
		
		// Register Hangfire jobs so in-process server can resolve them
		service.AddTransient<ExportJob>();
		service.AddTransient<ImportJob>();

		return service;
	}
}
