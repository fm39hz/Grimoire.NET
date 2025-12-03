namespace Grimoire.Api.Extension;

using JetBrains.Annotations;
using Serilog;
using Serilog.Events;

public static class LogExtension {
	[UsedImplicitly]
	public static IServiceCollection AddLog(this IServiceCollection services, WebApplicationBuilder builder) {
		Log.Logger = new LoggerConfiguration()
			.WriteTo.Console()
			.CreateBootstrapLogger();

		builder.Host.UseSerilog((context, provider, configuration) => configuration
			.ReadFrom.Configuration(context.Configuration)
			.ReadFrom.Services(provider)
			.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
			.MinimumLevel.Override("System", LogEventLevel.Warning)
			.Enrich.FromLogContext().WriteTo.Console());
		return services;
	}
}
