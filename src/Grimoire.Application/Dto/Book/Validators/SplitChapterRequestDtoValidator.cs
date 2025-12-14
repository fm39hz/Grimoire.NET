namespace Grimoire.Application.Dto.Book.Validators;

using FluentValidation;

public class SplitChapterRequestDtoValidator : AbstractValidator<SplitChapterRequestDto> {
	public SplitChapterRequestDtoValidator() {
		RuleFor(x => x.SplitPoints)
			.NotEmpty()
			.WithMessage("At least one split point is required");

		RuleForEach(x => x.SplitPoints)
			.SetValidator(new SplitPointDtoValidator());

		// Validate that segment indices are strictly increasing
		RuleFor(x => x.SplitPoints)
			.Must(HaveStrictlyIncreasingIndices)
			.WithMessage("Split point segment indices must be strictly increasing (no duplicates or overlaps)")
			.When(x => x.SplitPoints != null && x.SplitPoints.Count > 1);
	}

	private static bool HaveStrictlyIncreasingIndices(List<SplitPointDto> splitPoints) {
		for (var i = 1; i < splitPoints.Count; i++) {
			if (splitPoints[i].SegmentIndex <= splitPoints[i - 1].SegmentIndex) {
				return false;
			}
		}
		return true;
	}
}

public class SplitPointDtoValidator : AbstractValidator<SplitPointDto> {
	public SplitPointDtoValidator() {
		RuleFor(x => x.SegmentIndex)
			.GreaterThan(0)
			.WithMessage("SegmentIndex must be greater than 0 (cannot split at the beginning)");

		RuleFor(x => x.NewChapterTitle)
			.NotEmpty()
			.WithMessage("NewChapterTitle is required")
			.MaximumLength(500)
			.WithMessage("NewChapterTitle must not exceed 500 characters");
	}
}
