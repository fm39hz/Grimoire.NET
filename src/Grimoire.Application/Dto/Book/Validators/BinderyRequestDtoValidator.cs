namespace Grimoire.Application.Dto.Book.Validators;

using System;
using Domain.Common;
using FluentValidation;

public sealed class BinderyRequestDtoValidator : AbstractValidator<BinderyRequestDto> {
	public BinderyRequestDtoValidator() {
		RuleFor(x => x.Format)
			.IsInEnum()
			.WithMessage("Invalid export format.");

		RuleFor(x => x.Mode)
			.NotEmpty()
			.Must(mode => string.Equals(mode, "Anthology", StringComparison.OrdinalIgnoreCase) ||
			              string.Equals(mode, "Single", StringComparison.OrdinalIgnoreCase))
			.WithMessage("Mode must be 'Anthology' or 'Single'.");

		RuleFor(x => x.TargetVolumeIds)
			.NotEmpty()
			.When(x => string.Equals(x.Mode, "Single", StringComparison.OrdinalIgnoreCase))
			.WithMessage("TargetVolumeIds must not be empty when Mode is 'Single'.");

		RuleForEach(x => x.TargetVolumeIds)
			.Must(BeValidVolumeId)
			.When(x => x.TargetVolumeIds != null)
			.WithMessage("Invalid volume ID format.");
	}

	private static bool BeValidVolumeId(string volumeId) =>
		PrefixedId.TryToGuid(volumeId, EntityPrefix.Volume, out _);
}
