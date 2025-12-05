namespace Grimoire.Api.Extension;

using Application.Dto.Book.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;

public static class ValidationExtension {
	public static IServiceCollection AddValidation(this IServiceCollection services) {
		services.AddFluentValidationAutoValidation(config => {
			config.DisableDataAnnotationsValidation = true;
		});

		// Register all validators from the Application assembly
		services.AddValidatorsFromAssemblyContaining<CreateSeriesRequestDtoValidator>();

		return services;
	}
}
