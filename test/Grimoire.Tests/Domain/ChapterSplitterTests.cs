namespace Grimoire.Tests.Domain;

using System;
using System.Collections.Generic;
using System.Linq;
using Grimoire.Domain.Entity.Book;
using Grimoire.Domain.Entity.Book.Segment;
using Grimoire.Domain.Service;
using Xunit;

public sealed class ChapterSplitterTests {

	private static TextSegmentModel MakeSeg(string text, string? footnoteId = null) =>
		new() {
			Id = Guid.CreateVersion7(),
			Runs = [new TextRun(text) { FootnoteId = footnoteId }]
		};

	private static FootnoteSegmentModel MakeNote(Guid id) =>
		new() { Id = id, Segments = [new TextSegmentModel { Id = Guid.CreateVersion7(), Runs = [new TextRun("note")] }] };

	[Fact]
	public void Split_EmptySplitPoints_ThrowsArgumentException() {
		var chapter = new ChapterModel { Id = Guid.CreateVersion7(), Path = "n1.n2.n3", Order = 1, Title = "Original" };
		var segments = new List<SegmentModel> { MakeSeg("para") };
		Assert.Throws<ArgumentException>(() =>
			ChapterSplitter.Split(chapter, segments, []));
	}

	[Fact]
	public void Split_SegmentIndexOutOfBounds_ThrowsInvalidOperationException() {
		var chapter = new ChapterModel { Id = Guid.CreateVersion7(), Path = "n1.n2.n3", Order = 1, Title = "Original" };
		var segments = new List<SegmentModel> { MakeSeg("A"), MakeSeg("B") };
		Assert.Throws<InvalidOperationException>(() =>
			ChapterSplitter.Split(chapter, segments, [(5, "Beyond")]));
	}

	[Fact]
	public void Split_SingleSplitPoint_ProducesTwoChapters() {
		var chapter = new ChapterModel { Id = Guid.CreateVersion7(), Path = "n1.n2.n3", Order = 1, Title = "Original", Status = ChapterStatus.Done };
		var seg0 = MakeSeg("Para 0");
		var seg1 = MakeSeg("Para 1");
		var segments = new List<SegmentModel> { seg0, seg1 };

		var result = ChapterSplitter.Split(chapter, segments, [(1, "Part 2")]);

		Assert.Equal(2, result.NewChapters.Count);
		Assert.Equal(chapter.Id, result.UpdatedOriginal.Id);

		// Original keeps seg0, new chapter gets seg1
		var segs0 = result.UpdatedSegments.Where(s => s.Path.IsDescendantOf(result.UpdatedOriginal.Path)).ToList();
		var segs1 = result.UpdatedSegments.Where(s => s.Path.IsDescendantOf(result.NewChapters[1].Path)).ToList();

		Assert.Single(segs0);
		Assert.Equal("Para 0", ((TextSegmentModel)segs0[0]).Runs.First().Text);

		var newChapter = result.NewChapters[1];
		Assert.Equal("Part 2", newChapter.Title);
		Assert.Single(segs1);
		Assert.Equal("Para 1", ((TextSegmentModel)segs1[0]).Runs.First().Text);
		Assert.Equal(1.1d, newChapter.Order);
	}

	[Fact]
	public void Split_ThreeSplitPoints_ProducesFourChapters() {
		var chapter = new ChapterModel { Id = Guid.CreateVersion7(), Path = "n1.n2.n3", Order = 5, Title = "Original" };
		var segs = Enumerable.Range(0, 8).Select(static i => MakeSeg($"P{i}")).ToList();

		var result = ChapterSplitter.Split(chapter, segs, [(2, "Part2"), (4, "Part3"), (6, "Part4")]);

		Assert.Equal(4, result.NewChapters.Count);
		Assert.Equal(5.0d, result.NewChapters[0].Order);
		Assert.Equal(5.1d, result.NewChapters[1].Order);
		Assert.Equal(5.2d, result.NewChapters[2].Order);
		Assert.Equal(5.3d, result.NewChapters[3].Order);
	}
}
