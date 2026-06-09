namespace Grimoire.Application.Dto.Book.Validators;

using FluentValidation;

public class MergeChaptersRequestDtoValidator : AbstractValidator<MergeChaptersRequestDto> {
	public MergeChaptersRequestDtoValidator() {
		RuleFor(x => x.ChapterIds)
			.NotEmpty()
			.WithMessage("At least one chapter ID is required");

		RuleFor(x => x.ChapterIds)
			.Must(ids => ids.Count >= 2)
			.WithMessage("At least two chapters are required to merge")
			.When(x => x.ChapterIds is { Count: > 0 });

		RuleForEach(x => x.ChapterIds)
			.NotEmpty()
			.WithMessage("Chapter ID cannot be empty");
	}
}
