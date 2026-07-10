namespace Grimoire.Domain.Service;

using System;
using System.Collections.Generic;
using System.Linq;
using Entity.Book;
using Entity.Book.Segment;
using Common.ValueObject;

public static class ChapterSplitter {
	public record SplitResult(
		ChapterModel UpdatedOriginal, 
		IReadOnlyList<ChapterModel> NewChapters, 
		IReadOnlyList<SegmentModel> UpdatedSegments);

	public static SplitResult Split(
		ChapterModel original,
		IReadOnlyList<SegmentModel> allSegments,
		IReadOnlyList<(int SegmentIndex, string NewChapterTitle)> splitPoints) {

		if (allSegments.Count == 0) {
			throw new InvalidOperationException("Cannot split a chapter with no content");
		}

		if (splitPoints.Count == 0) {
			throw new ArgumentException("At least one split point is required", nameof(splitPoints));
		}

		// Separate normal segments from footnotes
		var contentSegments = allSegments.Where(s => s is not FootnoteSegmentModel).OrderBy(s => s.Order).ToList();
		var footnotes = allSegments.OfType<FootnoteSegmentModel>().ToList();

		foreach (var (segmentIndex, _) in splitPoints) {
			if (segmentIndex >= contentSegments.Count) {
				throw new InvalidOperationException(
					$"SegmentIndex {segmentIndex} is out of bounds (max: {contentSegments.Count - 1})");
			}
		}

		var resultChapters = new List<ChapterModel>();
		var updatedSegments = new List<SegmentModel>();
		const double orderIncrement = 0.1d;

		// Calculate parent path of original chapter using client-safe helper
		BookPath parentPath = original.Path.GetParent();

		var firstSplitIndex = splitPoints[0].SegmentIndex;
		
		// Original chapter keeps the first partition of content segments
		var firstPartition = contentSegments.Take(firstSplitIndex).ToList();
		var firstPartitionFootnotes = footnotes.PartitionFootnotes(firstPartition);

		// Keep original chapter's segments at their current path prefix, but they need to be returned as updated
		// because some footnotes might be removed from the original chapter.
		foreach (var seg in firstPartition) {
			seg.Path = $"{original.Path.Value}.n{seg.Id:N}";
			updatedSegments.Add(seg);
		}
		foreach (var fn in firstPartitionFootnotes) {
			fn.Path = $"{original.Path.Value}.n{fn.Id:N}";
			updatedSegments.Add(fn);
		}
		
		resultChapters.Add(original);
		var currentIndex = firstSplitIndex;

		for (var i = 0; i < splitPoints.Count; i++) {
			var (splitIndex, newTitle) = splitPoints[i];
			var nextIndex = i < splitPoints.Count - 1
				? splitPoints[i + 1].SegmentIndex
				: contentSegments.Count;

			var partition = contentSegments.Skip(currentIndex).Take(nextIndex - currentIndex).ToList();
			var partitionFootnotes = footnotes.PartitionFootnotes(partition);

			var newChapterId = Guid.CreateVersion7();
			var newChapterPath = $"{parentPath.Value}.n{newChapterId:N}";
			var newChapter = new ChapterModel {
				Id = newChapterId,
				Title = newTitle,
				Order = original.Order + (orderIncrement * (i + 1)),
				Status = original.Status,
				Path = newChapterPath
			};

			// Update paths of migrated content segments and footnotes
			foreach (var seg in partition) {
				seg.Path = $"{newChapterPath}.n{seg.Id:N}";
				updatedSegments.Add(seg);
			}
			foreach (var fn in partitionFootnotes) {
				fn.Path = $"{newChapterPath}.n{fn.Id:N}";
				updatedSegments.Add(fn);
			}

			resultChapters.Add(newChapter);
			currentIndex = nextIndex;
		}

		return new SplitResult(original, resultChapters.AsReadOnly(), updatedSegments.AsReadOnly());
	}

	private static List<FootnoteSegmentModel> PartitionFootnotes(
		this IReadOnlyList<FootnoteSegmentModel> allFootnotes,
		IEnumerable<SegmentModel> segments) {
		var referencedIds = segments.ExtractReferencedFootnoteIds();
		return allFootnotes
			.Where(f => referencedIds.Contains(f.Id.ToString()))
			.ToList();
	}

	private static HashSet<string> ExtractReferencedFootnoteIds(this IEnumerable<SegmentModel> segments) {
		var footnoteIds = new HashSet<string>();

		foreach (var segment in segments) {
			if (segment is not TextSegmentModel textSegment) {
				continue;
			}

			foreach (var run in textSegment.Runs) {
				if (!string.IsNullOrEmpty(run.FootnoteId)) {
					footnoteIds.Add(run.FootnoteId);
				}
			}
		}

		return footnoteIds;
	}
}
