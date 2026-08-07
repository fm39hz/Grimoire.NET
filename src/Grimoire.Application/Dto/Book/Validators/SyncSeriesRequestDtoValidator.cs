namespace Grimoire.Application.Dto.Book.Validators;

using FluentValidation;

public sealed class SyncSeriesRequestDtoValidator : AbstractValidator<SyncSeriesRequestDto> {
	public SyncSeriesRequestDtoValidator() {
		RuleFor(static x => x.Volumes)
			.NotEmpty()
			.WithMessage("At least one volume must be provided to sync.");

		RuleForEach(static x => x.Volumes).ChildRules(static volume => {
			volume.RuleFor(static v => v.Order)
				.GreaterThanOrEqualTo(0)
				.WithMessage("Volume order must be greater than or equal to 0.");
			volume.RuleFor(static v => v.Title)
				.NotEmpty()
				.WithMessage("Volume title is required.");

			volume.RuleForEach(static v => v.Chapters).ChildRules(static chapter => {
				chapter.RuleFor(static c => c.Order)
					.GreaterThanOrEqualTo(0)
					.WithMessage("Chapter order must be greater than or equal to 0.");
				chapter.RuleFor(static c => c.Title)
					.NotEmpty()
					.WithMessage("Chapter title is required.");

				chapter.RuleForEach(static c => c.Content)
					.SetValidator(new SegmentDtoValidator());
			});
		});
	}
}
