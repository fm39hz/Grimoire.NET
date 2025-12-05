namespace Grimoire.Api.Extension;

using Constant;
using JetBrains.Annotations;
using Microsoft.OpenApi.Models;

public static class NetworkExtension {
	[UsedImplicitly]
	public static IServiceCollection
		AddNetworkService(this IServiceCollection service, WebApplicationBuilder builder) {
		var serverUrl = builder.Configuration["OpenApi:ServerUrl"] ?? "http://localhost:8080";

		// Using Swashbuckle only to avoid polymorphic type conflicts in ASP.NET Core OpenAPI
		// builder.Services.AddOpenApi();
		builder.Services.AddSwaggerGen(static opt => {
			opt.SwaggerDoc(
				RouteConstant.VERSION,
				new OpenApiInfo { Title = $"{RouteConstant.PROJECT_NAME} API", Version = RouteConstant.VERSION }
				);
			opt.CustomSchemaIds(type => type.FullName);
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
