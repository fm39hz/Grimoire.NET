namespace Grimoire.Application.Dto.Book.Validators;

using FluentValidation;

public class UpdateChapterRequestDtoValidator : AbstractValidator<UpdateChapterRequestDto> {
	public UpdateChapterRequestDtoValidator() {
		When(x => x.Title != null, () => {
			RuleFor(x => x.Title)
				.NotEmpty()
				.WithMessage("Title cannot be empty when provided")
				.MaximumLength(500)
				.WithMessage("Title must not exceed 500 characters");
		});

		When(x => x.Order != null, () => {
			RuleFor(x => x.Order)
				.GreaterThanOrEqualTo(0)
				.WithMessage("Order must be greater than or equal to 0");
		});
	}
}
