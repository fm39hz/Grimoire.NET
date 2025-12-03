namespace Grimoire.Api;

using Domain.Constant;
using Extension;
using Infrastructure.Persistence.Database;

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
		builder.Services.AddLogging(static logging => logging.AddFilter(
				"Microsoft.EntityFrameworkCore.Database.Command",
				LogLevel.Warning)
			);
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddControllers();
		builder.Services.AddMvc();
		builder.Services.AddNetworkService(builder);
		builder.Services.AddDatabaseContext(builder);
		builder.Services.AddServices();
		builder.Services.AddStorage(builder.Configuration);

		return builder.Build();
	}
}
