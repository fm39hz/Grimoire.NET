namespace Grimoire.Domain.Service;

using System;
using System.Collections.Generic;
using System.Linq;
using Entity.Book;
using Entity.Book.Segment;

public static class ChapterMerger {
	public record MergeResult(
		ChapterModel BaseChapter,
		IReadOnlyList<SegmentModel> UpdatedSegments);

	public static MergeResult Merge(
		ChapterModel baseChapter,
		IReadOnlyList<SegmentModel> baseSegments,
		IReadOnlyList<(ChapterModel Chapter, IReadOnlyList<SegmentModel> Segments)> chaptersToMerge) {
		
		var updatedSegments = new List<SegmentModel>();
		
		// Sort base segments by Order
		var currentBaseSegments = baseSegments.OrderBy(s => s.Order).ToList();
		
		// Start new order offset from the end of the base chapter's content segments
		var contentSegments = currentBaseSegments.Where(s => s is not FootnoteSegmentModel).ToList();
		double currentOrderOffset = contentSegments.Count > 0 ? contentSegments.Max(s => s.Order) + 1.0 : 1.0;
		
		foreach (var (chapter, segments) in chaptersToMerge) {
			var chapterContentSegments = segments.Where(s => s is not FootnoteSegmentModel).OrderBy(s => s.Order).ToList();
			var chapterFootnotes = segments.OfType<FootnoteSegmentModel>().ToList();
			
			double originalMax = chapterContentSegments.Count > 0 ? chapterContentSegments.Max(s => s.Order) : 0.0;

			// Map chapter's content segments to the base chapter
			foreach (var seg in chapterContentSegments) {
				seg.Path = $"{baseChapter.Path}.n{seg.Id:N}";
				seg.Order += currentOrderOffset;
				updatedSegments.Add(seg);
			}
			
			// Map footnotes to the base chapter
			foreach (var fn in chapterFootnotes) {
				fn.Path = $"{baseChapter.Path}.n{fn.Id:N}";
				updatedSegments.Add(fn);
			}
			
			if (chapterContentSegments.Count > 0) {
				currentOrderOffset += originalMax + 1.0;
			}
		}
		
		return new MergeResult(baseChapter, updatedSegments);
	}
}
