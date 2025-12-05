namespace Grimoire.Application.Dto.Book.Validators;

using Application.Common;
using FluentValidation;

public class CreateVolumeRequestDtoValidator : AbstractValidator<CreateVolumeRequestDto> {
	public CreateVolumeRequestDtoValidator() {
		RuleFor(x => x.SeriesId)
			.NotEmpty()
			.WithMessage("SeriesId is required")
			.Must(BeValidSeriesId)
			.WithMessage("SeriesId must be a valid series ID with 'ser_' prefix (e.g., ser_01234567-89ab-cdef-0123-456789abcdef)");

		RuleFor(x => x.Title)
			.NotEmpty()
			.WithMessage("Title is required")
			.MaximumLength(500)
			.WithMessage("Title must not exceed 500 characters");

		RuleFor(x => x.Order)
			.GreaterThanOrEqualTo(0)
			.WithMessage("Order must be greater than or equal to 0");
	}

	private static bool BeValidSeriesId(string seriesId) {
		if (string.IsNullOrWhiteSpace(seriesId))
			return false;
			
		if (!seriesId.StartsWith($"{EntityPrefix.Series}_"))
			return false;
			
		return PrefixedId.TryToGuid(seriesId, out _);
	}
}
