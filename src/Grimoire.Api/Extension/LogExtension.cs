namespace Grimoire.Api.Extension;

using JetBrains.Annotations;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.SystemConsole.Themes;

public static class LogExtension {
	[UsedImplicitly]
	public static IServiceCollection AddLog(this IServiceCollection services, WebApplicationBuilder builder) {
		const string template =
			"{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception} {Properties:j}";
		Log.Logger = new LoggerConfiguration()
			.WriteTo.Console(outputTemplate : template).CreateBootstrapLogger();

		builder.Host.UseSerilog((context, provider, configuration) => configuration
			.ReadFrom.Configuration(context.Configuration)
			.ReadFrom.Services(provider)
			.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
			.MinimumLevel.Override("System", LogEventLevel.Warning)
			.Enrich.FromLogContext()
			.Enrich.WithExceptionDetails()
			.WriteTo.Async(a => a.Console(
				outputTemplate : template, theme : SystemConsoleTheme.Colored)));
		return services;
	}
}
