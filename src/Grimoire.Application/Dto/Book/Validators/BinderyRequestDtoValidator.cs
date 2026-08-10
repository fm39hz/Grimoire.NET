namespace Grimoire.Application.Dto.Book.Validators;

using System;
using Domain.Common;
using FluentValidation;

public sealed class BinderyRequestDtoValidator : AbstractValidator<BinderyRequestDto> {
	public BinderyRequestDtoValidator() {
		RuleFor(static x => x.Format)
			.IsInEnum()
			.WithMessage("Invalid export format.");

		RuleFor(static x => x.Mode)
			.NotEmpty()
			.Must(static mode => string.Equals(mode, "Anthology", StringComparison.OrdinalIgnoreCase) ||
						  string.Equals(mode, "Single", StringComparison.OrdinalIgnoreCase) ||
						  string.Equals(mode, "OnePerVolume", StringComparison.OrdinalIgnoreCase) ||
						  string.Equals(mode, "CustomGroups", StringComparison.OrdinalIgnoreCase))
			.WithMessage("Mode must be 'Anthology', 'Single', 'OnePerVolume', or 'CustomGroups'.");

		RuleFor(static x => x.TargetVolumeIds)
			.NotEmpty()
			.When(static x => string.Equals(x.Mode, "Single", StringComparison.OrdinalIgnoreCase))
			.WithMessage("TargetVolumeIds must not be empty when Mode is 'Single'.");

		RuleForEach(static x => x.TargetVolumeIds)
			.Must(BeValidVolumeId)
			.When(static x => x.TargetVolumeIds != null)
			.WithMessage("Invalid volume ID format.");

		RuleFor(static x => x.Groups)
			.NotEmpty()
			.When(static x => string.Equals(x.Mode, "CustomGroups", StringComparison.OrdinalIgnoreCase))
			.WithMessage("Groups must not be empty when Mode is 'CustomGroups'.");

		RuleForEach(static x => x.Groups).ChildRules(group => {
			group.RuleFor(static x => x.Name).NotEmpty();
			group.RuleFor(static x => x.TargetVolumeIds).NotEmpty();
			group.RuleForEach(static x => x.TargetVolumeIds).Must(BeValidVolumeId)
				.WithMessage("Invalid volume ID format.");
		});

		RuleFor(static x => x.Groups)
			.Must(static groups => groups is null || groups.Select(static group => group.Name.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase).Count() == groups.Count)
			.WithMessage("Group names must be unique.");
	}

	private static bool BeValidVolumeId(string volumeId) =>
		PrefixedId.TryToGuid(volumeId, EntityPrefix.Volume, out _);
}
