namespace Grimoire.Api;

using Constant;
using Extension;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Infrastructure.Configuration;
using Infrastructure.Persistence.Database;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Middleware;
using Serilog;
using Serilog.Events;
using Transformer;

public class Program {
	public static async Task Main(string[] args) {
		var builder = WebApplication.CreateBuilder(args);
		builder.Host.UseSerilog((_, lc) => lc
			.WriteTo.Console()
			.ReadFrom.Configuration(builder.Configuration));

		var app = Build(builder);

		app.UseMiddleware<GlobalExceptionMiddleware>();
		// app.UseHealthChecks("/health");

		if (app.Environment.IsDevelopment()) {
			{
				await using var scope = app.Services.CreateAsyncScope();
				await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
				await dbContext.Database.EnsureCreatedAsync();

				// EnsureCreatedAsync skips if DB already exists (Hangfire creates it first).
				// Probe our tables; if missing, recreate the full schema.
				try {
					await dbContext.Series.AnyAsync();
				}
				catch {
					await dbContext.Database.EnsureDeletedAsync();
					await dbContext.Database.EnsureCreatedAsync();
				}
			}

			app.UseSwagger(options => options.RouteTemplate = "openapi/{documentName}.json");
			app.UseSwaggerUI(static opt => {
				opt.SwaggerEndpoint($"/openapi/{RouteConstant.VERSION}.json",
					$"{RouteConstant.PROJECT_NAME} API {RouteConstant.VERSION}");
				opt.ConfigObject.PersistAuthorization = true;
				opt.DisplayRequestDuration();
			});
		}
		else {
			app.UseHsts();
			app.UseHttpsRedirection();
		}

		app.UseCors("AllowAll");
		
		// Start Hangfire server (singleton activated here)
		app.UseHangfireServer();
		
		if (app.Environment.IsDevelopment())
		{
			app.UseHangfireDashboard("/hangfire", new DashboardOptions
			{
				Authorization = []
			});
		}
		
		app.MapControllers();
		app.UseSerilogRequestLogging(options => options.GetLevel = (httpContext, _, ex) => {
			var path = httpContext.Request.Path.Value;

			return string.IsNullOrEmpty(path)
				? LogEventLevel.Information
				: path.StartsWith("/openapi")
					? ex != null || httpContext.Response.StatusCode >= 500
						? LogEventLevel.Error
						: LogEventLevel.Verbose
					: ex != null || httpContext.Response.StatusCode >= 500
						? LogEventLevel.Error
						: LogEventLevel.Information;
		});
		await app.RunAsync();
	}

	private static WebApplication Build(WebApplicationBuilder builder) {
		builder.Services.AddEndpointsApiExplorer();
		
		// Hangfire — enqueue + process jobs in-process
		// Multiple servers (e.g. +Grimoire.Job) cooperate via distributed locks
		builder.Services.AddHangfire(config => config
			.UsePostgreSqlStorage(options => options
				.UseNpgsqlConnection(
					builder.Configuration.GetConnectionString("Postgre")!)));
		builder.Services.AddHangfireServer(options =>
		{
			options.Queues = ["default", "exports"];
			options.WorkerCount = Math.Max(1, Environment.ProcessorCount);
		});
		
		builder.Services
			.AddControllers(options =>
				options.Conventions.Add(new RouteTokenTransformerConvention(new EndpointRouteTransformer())))
			.AddJsonOptions(options => {
				options.JsonSerializerOptions.PropertyNamingPolicy = JsonConfiguration.JsonOptions.PropertyNamingPolicy;
				options.JsonSerializerOptions.WriteIndented = JsonConfiguration.JsonOptions.WriteIndented;
				options.JsonSerializerOptions.ReferenceHandler = JsonConfiguration.JsonOptions.ReferenceHandler;
				options.JsonSerializerOptions.WriteIndented = JsonConfiguration.JsonOptions.WriteIndented;
				options.JsonSerializerOptions.AllowOutOfOrderMetadataProperties =
					JsonConfiguration.JsonOptions.AllowOutOfOrderMetadataProperties;
			});
		builder.Services.AddMvc();
		builder.Services.AddValidation();
		builder.Services.AddServices(builder);
		builder.Services.AddLog(builder);
		builder.Services.AddNetworkService(builder);
		builder.Services.AddDatabaseContext(builder);

		return builder.Build();
	}
}
