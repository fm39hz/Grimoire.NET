namespace Grimoire.Tests.Domain;

using Grimoire.Domain.Entity.Book;
using Grimoire.Domain.Entity.Book.Segment;
using Grimoire.Domain.Service;
using Xunit;

public sealed class ChapterSplitterTests {
	// ── helpers ────────────────────────────────────────────────────────────────

	private static ChapterModel MakeChapter(
		IList<SegmentModel> segments,
		IList<FootnoteSegmentModel>? footnotes = null,
		float order = 1f) {
		var id = Guid.CreateVersion7();
		return new ChapterModel {
			Id = id,
			VolumeId = Guid.CreateVersion7(),
			Order = order,
			Title = "Original",
			Status = ChapterStatus.Done,
			ContentData = new ChapterContentModel {
				Id = id,
				Segments = [.. segments],
				Footnotes = footnotes is null ? [] : [.. footnotes]
			}
		};
	}

	private static TextSegmentModel MakeSeg(string text, string? footnoteId = null) =>
		new() {
			Id = Guid.CreateVersion7(),
			Runs = [new TextRun(text) { FootnoteId = footnoteId }]
		};

	private static FootnoteSegmentModel MakeNote(Guid id) =>
		new() { Id = id, Segments = [new TextSegmentModel { Id = Guid.CreateVersion7(), Runs = [new TextRun("note")] }] };

	// ── guard clauses ──────────────────────────────────────────────────────────

	[Fact]
	public void Split_NoContent_ThrowsInvalidOperationException() {
		var chapter = new ChapterModel { Id = Guid.CreateVersion7(), VolumeId = Guid.CreateVersion7(), Order = 1, Title = "T" };
		Assert.Throws<InvalidOperationException>(() =>
			chapter.Split([(1, "New")]));
	}

	[Fact]
	public void Split_EmptySegments_ThrowsInvalidOperationException() {
		var id = Guid.CreateVersion7();
		var chapter = new ChapterModel {
			Id = id,
			VolumeId = Guid.CreateVersion7(),
			Order = 1,
			Title = "T",
			ContentData = new ChapterContentModel { Id = id, Segments = [], Footnotes = [] }
		};
		Assert.Throws<InvalidOperationException>(() =>
			chapter.Split([(0, "New")]));
	}

	[Fact]
	public void Split_EmptySplitPoints_ThrowsArgumentException() {
		var chapter = MakeChapter([MakeSeg("para")]);
		Assert.Throws<ArgumentException>(() =>
			chapter.Split([]));
	}

	[Fact]
	public void Split_SegmentIndexOutOfBounds_ThrowsInvalidOperationException() {
		var chapter = MakeChapter([MakeSeg("A"), MakeSeg("B")]);
		Assert.Throws<InvalidOperationException>(() =>
			chapter.Split([(5, "Beyond")]));
	}

	// ── single split ──────────────────────────────────────────────────────────

	[Fact]
	public void Split_SingleSplitPoint_ProducesTwoChapters() {
		var seg0 = MakeSeg("Para 0");
		var seg1 = MakeSeg("Para 1");
		var chapter = MakeChapter([seg0, seg1]);
		var originalId = chapter.Id;
		var originalOrder = chapter.Order;

		var result = chapter.Split([(1, "Part 2")]);

		Assert.Equal(2, result.NewChapters.Count);
		// original is first element
		Assert.Equal(originalId, result.UpdatedOriginal.Id);
		// original keeps seg0
		Assert.Equal([seg0], result.UpdatedOriginal.ContentData!.Segments);
		// new chapter gets seg1
		var newChapter = result.NewChapters[1];
		Assert.Equal("Part 2", newChapter.Title);
		Assert.Equal([seg1], newChapter.ContentData!.Segments);
		// order increment
		Assert.Equal(originalOrder + 0.1f, newChapter.Order, precision: 4);
	}

	[Fact]
	public void Split_AtIndexZero_OriginalHasNoSegments_NewChapterHasAll() {
		var seg0 = MakeSeg("A");
		var seg1 = MakeSeg("B");
		var chapter = MakeChapter([seg0, seg1]);

		var result = chapter.Split([(0, "All")]);

		Assert.Empty(result.UpdatedOriginal.ContentData!.Segments);
		Assert.Equal([seg0, seg1], result.NewChapters[1].ContentData!.Segments);
	}

	// ── multiple splits ────────────────────────────────────────────────────────

	[Fact]
	public void Split_ThreeSplitPoints_ProducesFourChapters() {
		var segs = Enumerable.Range(0, 8).Select(i => MakeSeg($"P{i}")).ToArray();
		var chapter = MakeChapter(segs, order: 5f);

		// split at 2, 4, 6
		var result = chapter.Split([(2, "Part2"), (4, "Part3"), (6, "Part4")]);

		Assert.Equal(4, result.NewChapters.Count);
		Assert.Equal(2, result.NewChapters[0].ContentData!.Segments.Count); // segs 0-1
		Assert.Equal(2, result.NewChapters[1].ContentData!.Segments.Count); // segs 2-3
		Assert.Equal(2, result.NewChapters[2].ContentData!.Segments.Count); // segs 4-5
		Assert.Equal(2, result.NewChapters[3].ContentData!.Segments.Count); // segs 6-7
	}

	[Fact]
	public void Split_OrderIncrements_AreCorrect() {
		var segs = Enumerable.Range(0, 4).Select(i => MakeSeg($"P{i}")).ToArray();
		var chapter = MakeChapter(segs, order: 3f);

		var result = chapter.Split([(1, "B"), (2, "C"), (3, "D")]);

		Assert.Equal(3f, result.NewChapters[0].Order, precision: 4);
		Assert.Equal(3.1f, result.NewChapters[1].Order, precision: 4);
		Assert.Equal(3.2f, result.NewChapters[2].Order, precision: 4);
		Assert.Equal(3.3f, result.NewChapters[3].Order, precision: 4);
	}

	// ── new chapter metadata ───────────────────────────────────────────────────

	[Fact]
	public void Split_NewChaptersInheritVolumeIdAndStatus() {
		var volumeId = Guid.CreateVersion7();
		var id = Guid.CreateVersion7();
		var chapter = new ChapterModel {
			Id = id,
			VolumeId = volumeId,
			Order = 1,
			Title = "T",
			Status = ChapterStatus.Done,
			ContentData = new ChapterContentModel { Id = id, Segments = [MakeSeg("A"), MakeSeg("B")], Footnotes = [] }
		};

		var result = chapter.Split([(1, "New")]);
		var newChapter = result.NewChapters[1];

		Assert.Equal(volumeId, newChapter.VolumeId);
		Assert.Equal(ChapterStatus.Done, newChapter.Status);
	}

	[Fact]
	public void Split_NewChapterContentId_MatchesChapterId() {
		var chapter = MakeChapter([MakeSeg("A"), MakeSeg("B")]);
		var result = chapter.Split([(1, "New")]);
		var newChapter = result.NewChapters[1];
		Assert.Equal(newChapter.Id, newChapter.ContentData!.Id);
	}

	// ── footnote partitioning ─────────────────────────────────────────────────

	[Fact]
	public void Split_FootnotesPartitioned_FollowTheirReferencingSegments() {
		var note1Id = Guid.CreateVersion7();
		var note2Id = Guid.CreateVersion7();
		var note1 = MakeNote(note1Id);
		var note2 = MakeNote(note2Id);

		// seg0 references note1, seg1 references note2
		var seg0 = MakeSeg("Para 0", note1Id.ToString());
		var seg1 = MakeSeg("Para 1", note2Id.ToString());

		var chapter = MakeChapter([seg0, seg1], [note1, note2]);

		var result = chapter.Split([(1, "Part2")]);

		// original gets only note1
		Assert.Equal([note1], result.UpdatedOriginal.ContentData!.Footnotes);
		// new chapter gets only note2
		Assert.Equal([note2], result.NewChapters[1].ContentData!.Footnotes);
	}

	[Fact]
	public void Split_UnreferencedFootnotes_AreExcludedFromBothParts() {
		var orphanNote = MakeNote(Guid.CreateVersion7());
		var chapter = MakeChapter([MakeSeg("A"), MakeSeg("B")], [orphanNote]);

		var result = chapter.Split([(1, "B")]);

		Assert.Empty(result.UpdatedOriginal.ContentData!.Footnotes);
		Assert.Empty(result.NewChapters[1].ContentData!.Footnotes);
	}
}
