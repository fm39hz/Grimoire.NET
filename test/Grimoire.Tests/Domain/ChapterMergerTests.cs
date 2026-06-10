namespace Grimoire.Tests.Domain;

using System.Linq;
using Grimoire.Domain.Entity.Book;
using Grimoire.Domain.Entity.Book.Segment;
using Grimoire.Domain.Service;
using Xunit;

public sealed class ChapterMergerTests {
	[Fact]
	public void Merge_BaseWithContent_And_OneSource_ConcatenatesSegmentsAndFootnotes() {
		var baseSegment = MakeTextSegment("Base para");
		var srcSegment = MakeTextSegment("Source para");
		var baseFootnote = MakeFootnote();
		var srcFootnote = MakeFootnote();

		var baseChapter = MakeChapter(new ChapterContentModel {
			Id = Guid.CreateVersion7(),
			Segments = [baseSegment],
			Footnotes = [baseFootnote]
		});
		var source = MakeChapter(new ChapterContentModel {
			Id = Guid.CreateVersion7(),
			Segments = [srcSegment],
			Footnotes = [srcFootnote]
		});

		var result = baseChapter.Merge([source]);

		Assert.Equal(2, result.ContentData!.Segments.Count);
		Assert.Equal(2, result.ContentData.Footnotes.Count);
		Assert.Same(baseSegment, result.ContentData.Segments[0]);
		Assert.Same(srcSegment, result.ContentData.Segments[1]);
	}

	[Fact]
	public void Merge_BaseWithNoContent_UsesSourceSegments() {
		var srcSegment = MakeTextSegment("Source only");
		var baseChapter = MakeChapter(null);
		var source = MakeChapter(new ChapterContentModel {
			Id = Guid.CreateVersion7(),
			Segments = [srcSegment],
			Footnotes = []
		});

		var result = baseChapter.Merge([source]);

		var seg = Assert.Single(result.ContentData!.Segments);
		Assert.Same(srcSegment, seg);
	}

	[Fact]
	public void Merge_SourceWithNoContent_IsSkipped() {
		var baseSegment = MakeTextSegment("Base only");
		var baseChapter = MakeChapter(new ChapterContentModel {
			Id = Guid.CreateVersion7(),
			Segments = [baseSegment],
			Footnotes = []
		});

		var result = baseChapter.Merge([MakeChapter(null)]);

		var seg = Assert.Single(result.ContentData!.Segments);
		Assert.Same(baseSegment, seg);
	}

	[Fact]
	public void Merge_MultipleSourcesInOrder_AppendedSequentially() {
		var baseChapter = MakeChapter(null);
		var src1 = MakeChapter(new ChapterContentModel { Id = Guid.CreateVersion7(), Segments = [MakeTextSegment("A")], Footnotes = [] });
		var src2 = MakeChapter(new ChapterContentModel { Id = Guid.CreateVersion7(), Segments = [MakeTextSegment("B")], Footnotes = [] });
		var src3 = MakeChapter(new ChapterContentModel { Id = Guid.CreateVersion7(), Segments = [MakeTextSegment("C")], Footnotes = [] });

		var result = baseChapter.Merge([src1, src2, src3]);

		Assert.Equal(3, result.ContentData!.Segments.Count);
		Assert.Equal("A", ((TextSegmentModel)result.ContentData.Segments[0]).Runs.First().Text);
		Assert.Equal("B", ((TextSegmentModel)result.ContentData.Segments[1]).Runs.First().Text);
		Assert.Equal("C", ((TextSegmentModel)result.ContentData.Segments[2]).Runs.First().Text);
	}

	[Fact]
	public void Merge_PreservesBaseChapterContentDataId() {
		var contentId = Guid.CreateVersion7();
		var baseChapter = MakeChapter(new ChapterContentModel { Id = contentId, Segments = [], Footnotes = [] });
		baseChapter.Merge([MakeChapter(null)]);
		Assert.Equal(contentId, baseChapter.ContentData!.Id);
	}

	[Fact]
	public void Merge_BaseWithNoContent_UsesChapterIdAsContentId() {
		var baseChapter = MakeChapter(null);
		baseChapter.Merge([MakeChapter(null)]);
		Assert.Equal(baseChapter.Id, baseChapter.ContentData!.Id);
	}

	private static ChapterModel MakeChapter(ChapterContentModel? content) =>
		new() { Id = Guid.CreateVersion7(), VolumeId = Guid.CreateVersion7(), Order = 1, Title = "Ch", ContentData = content };

	private static TextSegmentModel MakeTextSegment(string text) =>
		new() { Id = Guid.CreateVersion7(), Runs = [new TextRun(text)] };

	private static FootnoteSegmentModel MakeFootnote() =>
		new() { Id = Guid.CreateVersion7(), Segments = [] };
}
