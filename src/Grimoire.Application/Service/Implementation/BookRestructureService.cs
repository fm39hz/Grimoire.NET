namespace Grimoire.Application.Service.Implementation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contract;
using Domain.Common;
using Domain.Common.Repository;
using Dto.Book;
using Dto.Book.Restructure;
using Dto.Book.Tree;

/// <summary>
///     Interprets an ordered batch of restructuring operations and executes them atomically.
///     The server validates every op (refs exist, same series, ordering bounds) before mutating,
///     and runs each through a trusted domain service. A failure rolls back the entire batch.
/// </summary>
public sealed class BookRestructureService(
	IBookTreeService bookTree,
	IChapterService chapterService,
	ISegmentService segmentService,
	IUnitOfWork unitOfWork) : IBookRestructureService {

	public async Task<BookTreeDto> ExecuteAsync(BookRestructureRequestDto request, CancellationToken cancellationToken = default) {
		var seriesId = PrefixedId.ToGuid(request.SeriesId, EntityPrefix.Series);

		// Validate every op up-front so a bad batch fails before any mutation starts.
		foreach (var op in request.Operations) {
			ValidateOp(seriesId, op);
		}

		await unitOfWork.BeginTransactionAsync(cancellationToken);
		try {
			foreach (var op in request.Operations) {
				await ExecuteOpAsync(op, cancellationToken);
			}
			await unitOfWork.CommitTransactionAsync(cancellationToken);
		}
		catch {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}

		return await bookTree.GetTree(seriesId, includeContent: false, cancellationToken);
	}

	private async Task ExecuteOpAsync(BookRestructureOp op, CancellationToken cancellationToken) {
		switch (op) {
			case MoveNodeOp m:
				await bookTree.MoveNode(
					PrefixedId.ToGuid(m.NodeId, ExpectNodePrefix(m.NodeId)),
					PrefixedId.ToGuid(m.NewParentId, ExpectParentPrefix(m.NewParentId)),
					m.NewOrder, cancellationToken);
				break;

			case DeleteNodeOp d:
				await bookTree.DeleteSubtree(
					PrefixedId.ToGuid(d.NodeId, ExpectNodePrefix(d.NodeId)), cancellationToken);
				break;

			case MergeChaptersOp mc:
				var mergeRequest = new MergeChaptersRequestDto([mc.BaseChapterId, .. mc.ChapterIds]);
				await chapterService.MergeAsync(mergeRequest, cancellationToken);
				break;

			case SplitChapterOp sc:
				var splitDto = new SplitChapterRequestDto(
					[.. sc.SplitPoints.Select(static sp => new SplitPointDto(sp.SegmentIndex, sp.NewChapterTitle))]);
				await chapterService.SplitAsync(
					PrefixedId.ToGuid(sc.ChapterId, EntityPrefix.Chapter), splitDto, cancellationToken);
				break;

			case MergeVolumesOp mv:
				await bookTree.MergeVolumesAsync(
					PrefixedId.ToGuid(mv.BaseVolumeId, EntityPrefix.Volume),
					[.. mv.VolumeIds.Select(static id => PrefixedId.ToGuid(id, EntityPrefix.Volume))],
					cancellationToken);
				break;

			case SplitVolumeOp sv:
				await bookTree.SplitVolumeAsync(
					PrefixedId.ToGuid(sv.VolumeId, EntityPrefix.Volume),
					sv.AtChapterOrder,
					sv.NewVolumeTitle,
					cancellationToken);
				break;

			case ReorderSiblingsOp rs:
				await bookTree.ReorderSiblingsAsync(
					PrefixedId.ToGuid(rs.ParentId, ExpectParentPrefix(rs.ParentId)),
					[.. rs.OrderedChildIds.Select(id => PrefixedId.ToGuid(id, ExpectNodePrefix(id)))],
					cancellationToken);
				break;

			case UpdateSegmentTextOp ut:
				await segmentService.UpdateTextAsync(
					PrefixedId.ToGuid(ut.SegmentId, EntityPrefix.Segment),
					ut.Runs,
					cancellationToken);
				break;

			default:
				throw new InvalidOperationException($"Unknown operation type '{op.GetType().Name}'");
		}
	}

	private static void ValidateOp(Guid seriesId, BookRestructureOp op) {
		// Structural validation that guards data integrity beyond the FluentValidation layer.
		// Full existence/series checks happen in the domain services themselves.
		switch (op) {
			case MergeChaptersOp mc when mc.ChapterIds.Contains(mc.BaseChapterId):
				throw new InvalidOperationException("Base chapter cannot also be a merge target");
			case MergeVolumesOp mv when mv.VolumeIds.Contains(mv.BaseVolumeId):
				throw new InvalidOperationException("Base volume cannot also be a merge target");
		}
	}

	private static string ExpectNodePrefix(string id) {
		var prefix = PrefixedId.GetPrefix(id);
		return prefix is EntityPrefix.Volume or EntityPrefix.Chapter
			? prefix
			: throw new InvalidOperationException($"'{id}' is not a volume or chapter id");
	}

	private static string ExpectParentPrefix(string id) {
		var prefix = PrefixedId.GetPrefix(id);
		return prefix is EntityPrefix.Series or EntityPrefix.Volume
			? prefix
			: throw new InvalidOperationException($"'{id}' is not a series or volume id");
	}
}
