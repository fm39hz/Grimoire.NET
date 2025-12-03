namespace Grimoire.Api;

using System.Text.Json;
using Domain.Constant;
using Extension;
using Infrastructure.Configuration;
using Infrastructure.Persistence.Database;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Middleware;
using Serilog;
using Serilog.Events;
using Transformer;

public static class Program {
	public static async Task Main(string[] args) {
		var builder = WebApplication.CreateBuilder(args);
		builder.Host.UseSerilog((_, lc) => lc
			.WriteTo.Console()
			.ReadFrom.Configuration(builder.Configuration));

		var app = Build(builder);

		if (app.Environment.IsDevelopment()) {
			app.UseHsts();
			app.MapOpenApi();
			app.UseExceptionHandler(new ExceptionHandlerOptions {
				AllowStatusCode404Response = true, ExceptionHandlingPath = "/error"
			});
			{
				await using var scope = app.Services.CreateAsyncScope();
				await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
				await dbContext.Database.EnsureCreatedAsync();
			}

			app.UseSwagger();
			app.UseSwaggerUI(static opt => {
				opt.SwaggerEndpoint($"/openapi/{RouteConstant.VERSION}.json",
					$"{RouteConstant.PROJECT_NAME} API {RouteConstant.VERSION}");
				opt.ConfigObject.PersistAuthorization = true;
				opt.DisplayRequestDuration();
			});
		}
		else {
			app.UseHttpsRedirection();
		}

		app.UseCors("AllowAll");
		app.MapControllers();
		app.UseSerilogRequestLogging(options => {
			options.GetLevel = (httpContext, _, ex) => {
				var path = httpContext.Request.Path.Value;

				if (string.IsNullOrEmpty(path)) {
					return LogEventLevel.Information;
				}

				if (path.StartsWith("/openapi")) {
					return ex != null || httpContext.Response.StatusCode >= 500
						? LogEventLevel.Error
						: LogEventLevel.Verbose;
				}

				return ex != null || httpContext.Response.StatusCode >= 500
					? LogEventLevel.Error
					: LogEventLevel.Information;
			};
		});
		app.UseMiddleware<PaginationMiddleware>();
		await app.RunAsync();
	}

	private static WebApplication Build(WebApplicationBuilder builder) {
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddControllers(options => {
			options.Conventions.Add(new RouteTokenTransformerConvention(new KebabCaseTransformer()));
		}).AddJsonOptions(options => {
			options.JsonSerializerOptions.PropertyNamingPolicy = JsonConfiguration.JsonOptions.PropertyNamingPolicy;
			options.JsonSerializerOptions.WriteIndented = JsonConfiguration.JsonOptions.WriteIndented;
			options.JsonSerializerOptions.ReferenceHandler = JsonConfiguration.JsonOptions.ReferenceHandler;
			options.JsonSerializerOptions.WriteIndented = JsonConfiguration.JsonOptions.WriteIndented;
			options.JsonSerializerOptions.AllowOutOfOrderMetadataProperties =
				JsonConfiguration.JsonOptions.AllowOutOfOrderMetadataProperties;
		});
		builder.Services.AddMvc();
		builder.Services.AddServices();
		builder.Services.AddLog(builder);
		builder.Services.AddStorage(builder);
		builder.Services.AddNetworkService(builder);
		builder.Services.AddDatabaseContext(builder);

		return builder.Build();
	}
}
