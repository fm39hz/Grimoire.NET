namespace Grimoire.Application.Dto.Book.Validators;

using FluentValidation;

public class MergeChaptersRequestDtoValidator : AbstractValidator<MergeChaptersRequestDto> {
	public MergeChaptersRequestDtoValidator() {
		RuleFor(static x => x.ChapterIds)
			.NotEmpty()
			.WithMessage("At least one chapter ID is required");

		RuleFor(static x => x.ChapterIds)
			.Must(static ids => ids.Count >= 2)
			.WithMessage("At least two chapters are required to merge")
			.When(static x => x.ChapterIds is { Count: > 0 });

		RuleForEach(static x => x.ChapterIds)
			.NotEmpty()
			.WithMessage("Chapter ID cannot be empty");
	}
}
