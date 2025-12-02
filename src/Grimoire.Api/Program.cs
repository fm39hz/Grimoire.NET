namespace Grimoire.Api;

using Domain.Constant;
using Extension;
using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

public static class Program {
	public static async Task Main(string[] args) {
		var builder = WebApplication.CreateBuilder(args);
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
		await app.RunAsync();
	}

	private static WebApplication Build(WebApplicationBuilder builder) {
		var serverUrl = builder.Configuration["OpenApi:ServerUrl"] ?? "http://localhost:8080";

		builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) => {
			document.Servers = [
				new OpenApiServer { Url = serverUrl, Description = $"{RouteConstant.PROJECT_NAME} API Server" }
			];
			return Task.CompletedTask;
		}));
		builder.Services.AddLogging(static logging => logging.AddFilter(
				"Microsoft.EntityFrameworkCore.Database.Command",
				LogLevel.Warning)
			);
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddControllers();
		builder.Services.AddMvc();
		builder.Services.AddDbContext<ApplicationDbContext>(options =>
			options.UseNpgsql(builder.Configuration.GetConnectionString("Postgre"))
				.ConfigureWarnings(w => w.Ignore(CoreEventId.AccidentalEntityType)));
		builder.Services.AddSwaggerGen(static opt => {
			opt.SwaggerDoc(
				RouteConstant.VERSION,
				new OpenApiInfo { Title = $"{RouteConstant.PROJECT_NAME} API", Version = RouteConstant.VERSION }
				);
		});
		builder.Services.AddCors(options => {
			options.AddPolicy("AllowAll", policy => {
				policy.AllowAnyOrigin()
					.AllowAnyHeader()
					.AllowAnyMethod();
			});
		});
		builder.Services.AddServices();

		return builder.Build();
	}
}
