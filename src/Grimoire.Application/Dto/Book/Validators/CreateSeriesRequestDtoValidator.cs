namespace Grimoire.Application.Dto.Book.Validators;

using FluentValidation;

public class CreateSeriesRequestDtoValidator : AbstractValidator<CreateSeriesRequestDto> {
	public CreateSeriesRequestDtoValidator() {
		RuleFor(static x => x.Title)
			.NotEmpty()
			.WithMessage("Title is required")
			.MaximumLength(500)
			.WithMessage("Title must not exceed 500 characters");

		RuleFor(static x => x.Metadata)
			.NotNull()
			.WithMessage("Metadata is required");
	}
}
