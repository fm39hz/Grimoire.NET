namespace Grimoire.Api.Extension;

using System.Diagnostics;
using System.Linq;
using Application.Dto.Book.Validators;
using Constant;
using FluentValidation;
using FluentValidation.AspNetCore;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public static class ValidationExtension {
	[UsedImplicitly]
	public static IServiceCollection AddValidation(this IServiceCollection services) {
		services.AddFluentValidationAutoValidation(static config => config.DisableDataAnnotationsValidation = true);

		services.AddValidatorsFromAssemblyContaining<CreateSeriesRequestDtoValidator>();

		services.Configure<MvcOptions>(static options =>
			options.ModelMetadataDetailsProviders.Add(new LTreeValidationMetadataProvider()));

		services.Configure<ApiBehaviorOptions>(static options => options.InvalidModelStateResponseFactory = static context => {
			var errors = context.ModelState
				.Where(static e => e.Value?.Errors.Count > 0)
				.ToDictionary(
					static kvp => kvp.Key,
					static kvp => kvp.Value!.Errors.Select(static x => x.ErrorMessage).ToArray()
				);

			var problemDetails = new ProblemDetails {
				Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
				Title = "One or more validation errors occurred.",
				Status = StatusCodes.Status400BadRequest,
				Extensions = {
						[ProblemDetailsKeys.Errors] = errors,
						[ProblemDetailsKeys.TraceId] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier
					}
			};

			return new BadRequestObjectResult(problemDetails) {
				ContentTypes = { ContentTypes.ProblemJson }
			};
		});

		return services;
	}
}
