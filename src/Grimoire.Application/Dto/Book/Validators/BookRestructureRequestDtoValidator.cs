namespace Grimoire.Application.Dto.Book.Validators;

using Domain.Common;
using FluentValidation;
using Restructure;

public sealed class BookRestructureRequestDtoValidator : AbstractValidator<BookRestructureRequestDto> {
	public BookRestructureRequestDtoValidator() {
		RuleFor(static x => x.SeriesId)
			.NotEmpty()
			.WithMessage("SeriesId is required")
			.Must(BeValidSeriesId)
			.WithMessage("SeriesId must be a valid series ID with 'ser_' prefix");

		RuleFor(static x => x.Operations)
			.NotEmpty()
			.WithMessage("At least one operation is required");

		RuleForEach(static x => x.Operations).ChildRules(static op => {
			op.RuleFor(static o => o).SetInheritanceValidator(v => {
				v.Add(new MoveNodeOpValidator());
				v.Add(new DeleteNodeOpValidator());
				v.Add(new MergeChaptersOpValidator());
				v.Add(new SplitChapterOpValidator());
				v.Add(new MergeVolumesOpValidator());
				v.Add(new SplitVolumeOpValidator());
				v.Add(new ReorderSiblingsOpValidator());
				v.Add(new UpdateSegmentTextOpValidator());
			});
		});
	}

	private static bool BeValidSeriesId(string seriesId) => PrefixedId.TryToGuid(seriesId, EntityPrefix.Series, out _);

	public sealed class MoveNodeOpValidator : AbstractValidator<MoveNodeOp> {
		public MoveNodeOpValidator() {
			RuleFor(static x => x.NodeId).Must(BeValidNodeId).WithMessage("NodeId must be a valid vol_/chp_ id");
			RuleFor(static x => x.NewParentId).Must(BeValidParentId).WithMessage("NewParentId must be a valid vol_/ser_ id");
			RuleFor(static x => x.NewOrder).GreaterThanOrEqualTo(0).WithMessage("NewOrder must be >= 0");
		}
	}

	public sealed class DeleteNodeOpValidator : AbstractValidator<DeleteNodeOp> {
		public DeleteNodeOpValidator() {
			RuleFor(static x => x.NodeId).Must(BeValidNodeId).WithMessage("NodeId must be a valid vol_/chp_ id");
		}
	}

	public sealed class MergeChaptersOpValidator : AbstractValidator<MergeChaptersOp> {
		public MergeChaptersOpValidator() {
			RuleFor(static x => x.ChapterIds).NotEmpty().WithMessage("At least one chapter to merge is required");
			RuleFor(static x => x.BaseChapterId).Must(BeValidChapterId).WithMessage("BaseChapterId must be a valid chp_ id");
			RuleForEach(static x => x.ChapterIds).Must(BeValidChapterId).WithMessage("Each chapter must be a valid chp_ id");
		}
	}

	public sealed class SplitChapterOpValidator : AbstractValidator<SplitChapterOp> {
		public SplitChapterOpValidator() {
			RuleFor(static x => x.ChapterId).Must(BeValidChapterId).WithMessage("ChapterId must be a valid chp_ id");
			RuleFor(static x => x.SplitPoints).NotEmpty().WithMessage("At least one split point is required");
			RuleForEach(static x => x.SplitPoints).ChildRules(static sp => {
				sp.RuleFor(static s => s.SegmentIndex).GreaterThanOrEqualTo(0).WithMessage("SegmentIndex must be >= 0");
				sp.RuleFor(static s => s.NewChapterTitle).NotEmpty().WithMessage("NewChapterTitle is required");
			});
		}
	}

	public sealed class MergeVolumesOpValidator : AbstractValidator<MergeVolumesOp> {
		public MergeVolumesOpValidator() {
			RuleFor(static x => x.VolumeIds).NotEmpty().WithMessage("At least one volume to merge is required");
			RuleFor(static x => x.BaseVolumeId).Must(BeValidVolumeId).WithMessage("BaseVolumeId must be a valid vol_ id");
			RuleForEach(static x => x.VolumeIds).Must(BeValidVolumeId).WithMessage("Each volume must be a valid vol_ id");
		}
	}

	public sealed class SplitVolumeOpValidator : AbstractValidator<SplitVolumeOp> {
		public SplitVolumeOpValidator() {
			RuleFor(static x => x.VolumeId).Must(BeValidVolumeId).WithMessage("VolumeId must be a valid vol_ id");
			RuleFor(static x => x.AtChapterOrder).GreaterThanOrEqualTo(0).WithMessage("AtChapterOrder must be >= 0");
			RuleFor(static x => x.NewVolumeTitle).NotEmpty().WithMessage("NewVolumeTitle is required");
		}
	}

	public sealed class ReorderSiblingsOpValidator : AbstractValidator<ReorderSiblingsOp> {
		public ReorderSiblingsOpValidator() {
			RuleFor(static x => x.ParentId).Must(BeValidParentId).WithMessage("ParentId must be a valid ser_/vol_ id");
			RuleFor(static x => x.OrderedChildIds).NotEmpty().WithMessage("OrderedChildIds must not be empty");
			RuleForEach(static x => x.OrderedChildIds).Must(BeValidNodeId).WithMessage("Each child must be a valid vol_/chp_ id");
		}
	}

	public sealed class UpdateSegmentTextOpValidator : AbstractValidator<UpdateSegmentTextOp> {
		public UpdateSegmentTextOpValidator() {
			RuleFor(static x => x.SegmentId).Must(BeValidSegmentId).WithMessage("SegmentId must be a valid seg_ id");
			RuleFor(static x => x.Runs).NotEmpty().WithMessage("Runs must not be empty");
			RuleForEach(static x => x.Runs).ChildRules(static run => {
				run.RuleFor(static r => r.Text).NotEmpty().WithMessage("Each run must have non-empty text");
			});
		}
	}

	private static bool BeValidNodeId(string? id) =>
		PrefixedId.TryToGuid(id, EntityPrefix.Volume, out _) || PrefixedId.TryToGuid(id, EntityPrefix.Chapter, out _);

	private static bool BeValidParentId(string? id) =>
		PrefixedId.TryToGuid(id, EntityPrefix.Series, out _) || PrefixedId.TryToGuid(id, EntityPrefix.Volume, out _);

	private static bool BeValidVolumeId(string? id) => PrefixedId.TryToGuid(id, EntityPrefix.Volume, out _);
	private static bool BeValidChapterId(string? id) => PrefixedId.TryToGuid(id, EntityPrefix.Chapter, out _);
	private static bool BeValidSegmentId(string? id) => PrefixedId.TryToGuid(id, EntityPrefix.Segment, out _);
}
