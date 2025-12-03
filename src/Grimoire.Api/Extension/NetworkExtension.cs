namespace Grimoire.Api.Extension;

using Constant;
using JetBrains.Annotations;
using Microsoft.OpenApi.Models;

public static class NetworkExtension {
	[UsedImplicitly]
	public static IServiceCollection
		AddNetworkService(this IServiceCollection service, WebApplicationBuilder builder) {
		var serverUrl = builder.Configuration["OpenApi:ServerUrl"] ?? "http://localhost:8080";

		builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) => {
			document.Servers = [
				new OpenApiServer { Url = serverUrl, Description = $"{RouteConstant.PROJECT_NAME} API Server" }
			];
			return Task.CompletedTask;
		}));
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
		return service;
	}
}
