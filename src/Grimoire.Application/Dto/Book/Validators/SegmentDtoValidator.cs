namespace Grimoire.Application.Dto.Book.Validators;

using FluentValidation;
using Segment;

/// <summary>
///     Validates a wire-level polymorphic segment before it reaches the ingestion pipeline.
///     Runs per <c>$type</c>: text runs must be non-empty, image segments need a valid asset key,
///     footnote segments validate their nested text runs.
/// </summary>
public sealed class SegmentDtoValidator : AbstractValidator<SegmentDto> {
	public SegmentDtoValidator() {
		RuleFor(static s => s.Id)
			.Must(static id => string.IsNullOrEmpty(id) || Guid.TryParse(id, out _))
			.WithMessage("Segment id must be a valid UUID when provided.");

		RuleFor(static s => s).SetInheritanceValidator(v => {
			v.Add(new TextSegmentDtoValidator());
			v.Add(new ImageSegmentDtoValidator());
			v.Add(new DividerSegmentDtoValidator());
			v.Add(new FootnoteSegmentDtoValidator());
		});
	}

	public sealed class TextSegmentDtoValidator : AbstractValidator<TextSegmentDto> {
		public TextSegmentDtoValidator() {
			RuleFor(static t => t.Runs)
				.NotEmpty()
				.WithMessage("A text segment must contain at least one run.");
			RuleForEach(static t => t.Runs).ChildRules(static run => {
				run.RuleFor(static r => r.Text)
					.NotEmpty()
					.WithMessage("A text run must have non-empty text.");
				run.RuleFor(static r => r.FootnoteId)
					.Must(static id => string.IsNullOrEmpty(id) || Guid.TryParse(id, out _))
					.WithMessage("FootnoteId must be a valid UUID when provided.");
			});
		}
	}

	public sealed class ImageSegmentDtoValidator : AbstractValidator<ImageSegmentDto> {
		public ImageSegmentDtoValidator() {
			RuleFor(static i => i.AssetKey)
				.NotEmpty()
				.WithMessage("An image segment must reference an asset key.");
		}
	}

	public sealed class DividerSegmentDtoValidator : AbstractValidator<DividerSegmentDto> {
		public DividerSegmentDtoValidator() {
			RuleFor(static d => d.Style)
				.NotEmpty()
				.WithMessage("A divider segment must have a style.");
		}
	}

	public sealed class FootnoteSegmentDtoValidator : AbstractValidator<FootnoteSegmentDto> {
		public FootnoteSegmentDtoValidator() {
			RuleForEach(static f => f.Segments)
				.SetValidator(new TextSegmentDtoValidator());
		}
	}
}
