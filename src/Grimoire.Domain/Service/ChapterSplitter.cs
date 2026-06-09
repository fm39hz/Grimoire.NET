namespace Grimoire.Domain.Service;

using Entity.Book;
using Entity.Book.Segment;

public static class ChapterSplitter {
	public record SplitResult(ChapterModel UpdatedOriginal, IReadOnlyList<ChapterModel> NewChapters);

	public static SplitResult Split(
		this ChapterModel original,
		IReadOnlyList<(int SegmentIndex, string NewChapterTitle)> splitPoints) {
		var contentData = original.ContentData
						?? throw new InvalidOperationException("Cannot split a chapter with no content");

		var segments = contentData.Segments;

		if (segments.Count == 0) {
			throw new InvalidOperationException("Cannot split a chapter with no content");
		}
		}

		if (splitPoints.Count == 0) {
			throw new ArgumentException("At least one split point is required", nameof(splitPoints));
		}

		foreach (var (segmentIndex, _) in splitPoints) {
			if (segmentIndex >= segments.Count) {
				throw new InvalidOperationException(
					$"SegmentIndex {segmentIndex} is out of bounds (max: {segments.Count - 1})");
			}
		}

		var footnotes = contentData.Footnotes;
		var resultChapters = new List<ChapterModel>();
		const float orderIncrement = 0.1f;

		var firstSplitIndex = splitPoints[0].SegmentIndex;
		original.ContentData = new ChapterContentModel {
			Id = contentData.Id,
			Segments = segments.Take(firstSplitIndex).ToList(),
			Footnotes = footnotes.PartitionFootnotes(segments.Take(firstSplitIndex))
		};
		resultChapters.Add(original);
		var currentIndex = firstSplitIndex;

		for (var i = 0; i < splitPoints.Count; i++) {
			var (splitIndex, newTitle) = splitPoints[i];
			var nextIndex = i < splitPoints.Count - 1
				? splitPoints[i + 1].SegmentIndex
				: segments.Count;

			var newSegments = segments.Skip(currentIndex).Take(nextIndex - currentIndex).ToList();

			var newChapterId = Guid.CreateVersion7();
			var newChapter = new ChapterModel {
				Id = newChapterId,
				VolumeId = original.VolumeId,
				Title = newTitle,
				Order = original.Order + (orderIncrement * (i + 1)),
				Status = original.Status,
				ContentData = new ChapterContentModel {
					Id = newChapterId,
					Segments = newSegments,
					Footnotes = footnotes.PartitionFootnotes(newSegments)
				}
			};

			resultChapters.Add(newChapter);
			currentIndex = nextIndex;
		}

		return new SplitResult(original, resultChapters.AsReadOnly());
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
