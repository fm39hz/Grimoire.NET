namespace Grimoire.Api.Extension;

using Application.Dto.Book.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using JetBrains.Annotations;

public static class ValidationExtension {
	[UsedImplicitly]
	public static IServiceCollection AddValidation(this IServiceCollection services) {
		services.AddFluentValidationAutoValidation(config => config.DisableDataAnnotationsValidation = true);

		services.AddValidatorsFromAssemblyContaining<CreateSeriesRequestDtoValidator>();

		return services;
	}
}
