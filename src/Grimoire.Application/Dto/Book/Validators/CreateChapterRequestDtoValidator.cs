namespace Grimoire.Application.Dto.Book.Validators;

using Domain.Common;
using FluentValidation;

public class CreateChapterRequestDtoValidator : AbstractValidator<CreateChapterRequestDto> {
	public CreateChapterRequestDtoValidator() {
		RuleFor(x => x.VolumeId)
			.NotEmpty()
			.WithMessage("VolumeId is required")
			.Must(BeValidVolumeId)
			.WithMessage(
				"VolumeId must be a valid volume ID with 'vol_' prefix");

		RuleFor(x => x.Title)
			.NotEmpty()
			.WithMessage("Title is required")
			.MaximumLength(500)
			.WithMessage("Title must not exceed 500 characters");

		RuleFor(x => x.Order)
			.GreaterThanOrEqualTo(0)
			.WithMessage("Order must be greater than or equal to 0");

		// Either Content OR RawContent must be provided (XOR validation)
		RuleFor(x => x)
			.Must(x => x.Content != null || !string.IsNullOrEmpty(x.RawContent))
			.WithMessage("Either Content or RawContent must be provided")
			.Must(x => !(x.Content != null && !string.IsNullOrEmpty(x.RawContent)))
			.WithMessage("Cannot provide both Content and RawContent - only one is allowed");
	}

	private static bool BeValidVolumeId(string volumeId) => PrefixedId.TryToGuid(volumeId, EntityPrefix.Volume, out _);
}
