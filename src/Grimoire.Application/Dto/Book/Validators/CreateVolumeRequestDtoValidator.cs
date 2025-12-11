namespace Grimoire.Application.Dto.Book.Validators;

using Domain.Common;
using FluentValidation;

public class CreateVolumeRequestDtoValidator : AbstractValidator<CreateVolumeRequestDto> {
	public CreateVolumeRequestDtoValidator() {
		RuleFor(x => x.SeriesId)
			.NotEmpty()
			.WithMessage("SeriesId is required")
			.Must(BeValidSeriesId)
			.WithMessage(
				"SeriesId must be a valid series ID with 'ser_' prefix");

		RuleFor(x => x.Title)
			.NotEmpty()
			.WithMessage("Title is required")
			.MaximumLength(500)
			.WithMessage("Title must not exceed 500 characters");

		RuleFor(x => x.Order)
			.GreaterThanOrEqualTo(0)
			.WithMessage("Order must be greater than or equal to 0");
	}

	private static bool BeValidSeriesId(string seriesId) => PrefixedId.TryToGuid(seriesId, EntityPrefix.Series, out _);
}
