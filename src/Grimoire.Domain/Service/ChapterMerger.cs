namespace Grimoire.Domain.Service;

using Entity.Book;
using Entity.Book.Segment;

public static class ChapterMerger {
	public static ChapterModel Merge(
		this ChapterModel baseChapter,
		IReadOnlyList<ChapterModel> chaptersToMerge) {
		var allSegments = new List<SegmentModel>();
		var allFootnotes = new List<FootnoteSegmentModel>();

		if (baseChapter.ContentData is not null) {
			allSegments.AddRange(baseChapter.ContentData.Segments);
			allFootnotes.AddRange(baseChapter.ContentData.Footnotes);
		}

		foreach (var source in chaptersToMerge) {
			if (source.ContentData is null) {
				continue;
			}

			allSegments.AddRange(source.ContentData.Segments);
			allFootnotes.AddRange(source.ContentData.Footnotes);
		}

		baseChapter.ContentData = new ChapterContentModel {
			Id = baseChapter.ContentData?.Id ?? baseChapter.Id,
			Segments = allSegments,
			Footnotes = allFootnotes
		};

		return baseChapter;
	}
}
