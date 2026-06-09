namespace Grimoire.Application.Dto.Book.Validators;

using FluentValidation;

public class UpdateSeriesRequestDtoValidator : AbstractValidator<UpdateSeriesRequestDto> {
	public UpdateSeriesRequestDtoValidator() {
		When(x => x.Title != null, () => RuleFor(x => x.Title)
			.NotEmpty()
			.WithMessage("Title cannot be empty when provided")
			.MaximumLength(500)
			.WithMessage("Title must not exceed 500 characters"));
	}
}
