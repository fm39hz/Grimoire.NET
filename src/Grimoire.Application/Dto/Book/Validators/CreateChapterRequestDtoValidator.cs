namespace Grimoire.Application.Dto.Book.Validators;

using Application.Common;
using FluentValidation;

public class CreateChapterRequestDtoValidator : AbstractValidator<CreateChapterRequestDto> {
	public CreateChapterRequestDtoValidator() {
		RuleFor(x => x.VolumeId)
			.NotEmpty()
			.WithMessage("VolumeId is required")
			.Must(BeValidVolumeId)
			.WithMessage("VolumeId must be a valid volume ID with 'vol_' prefix (e.g., vol_01234567-89ab-cdef-0123-456789abcdef)");

		RuleFor(x => x.Title)
			.NotEmpty()
			.WithMessage("Title is required")
			.MaximumLength(500)
			.WithMessage("Title must not exceed 500 characters");

		RuleFor(x => x.Order)
			.GreaterThanOrEqualTo(0)
			.WithMessage("Order must be greater than or equal to 0");

		RuleFor(x => x.Content)
			.NotNull()
			.WithMessage("Content is required");
	}

	private static bool BeValidVolumeId(string volumeId) {
		if (string.IsNullOrWhiteSpace(volumeId))
			return false;
			
		if (!volumeId.StartsWith($"{EntityPrefix.Volume}_"))
			return false;
			
		return PrefixedId.TryToGuid(volumeId, out _);
	}
}
