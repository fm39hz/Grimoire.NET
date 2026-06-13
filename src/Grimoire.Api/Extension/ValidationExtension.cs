namespace Grimoire.Api.Extension;

using System.Diagnostics;
using System.Linq;
using Application.Dto.Book.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public static class ValidationExtension {
	[UsedImplicitly]
	public static IServiceCollection AddValidation(this IServiceCollection services) {
		services.AddFluentValidationAutoValidation(config => config.DisableDataAnnotationsValidation = true);

		services.AddValidatorsFromAssemblyContaining<CreateSeriesRequestDtoValidator>();

		services.Configure<ApiBehaviorOptions>(options => {
			options.InvalidModelStateResponseFactory = context => {
				var errors = context.ModelState
					.Where(e => e.Value?.Errors.Count > 0)
					.ToDictionary(
						kvp => kvp.Key,
						kvp => kvp.Value!.Errors.Select(x => x.ErrorMessage).ToArray()
					);

				var problemDetails = new ProblemDetails {
					Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
					Title = "One or more validation errors occurred.",
					Status = StatusCodes.Status400BadRequest,
					Extensions = {
						["errors"] = errors,
						["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier
					}
				};

				return new BadRequestObjectResult(problemDetails) {
					ContentTypes = { "application/problem+json" }
				};
			};
		});

		return services;
	}
}
