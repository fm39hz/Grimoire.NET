namespace Grimoire.Tests.Domain;

using System;
using System.Linq;
using Grimoire.Domain.Entity.Book;
using Grimoire.Domain.Entity.Book.Segment;
using Grimoire.Domain.Service;
using Xunit;

public sealed class ChapterMergerTests {
	[Fact]
	public void Merge_BaseWithContent_And_OneSource_ConcatenatesSegmentsAndUpdatesPaths() {
		var baseChapter = new ChapterModel { Id = Guid.CreateVersion7(), Path = "n1.n2.n3", Order = 1, Title = "Base" };
		var baseSegment = MakeTextSegment("Base para");
		baseSegment.Path = $"{baseChapter.Path}.ns1";
		baseSegment.Order = 1;

		var sourceChapter = new ChapterModel { Id = Guid.CreateVersion7(), Path = "n1.n2.n4", Order = 2, Title = "Source" };
		var srcSegment = MakeTextSegment("Source para");
		srcSegment.Path = $"{sourceChapter.Path}.ns2";

		var result = ChapterMerger.Merge(
			baseChapter,
			[baseSegment],
			[(sourceChapter, [srcSegment])]
		);

		// Both the base segment and the merged source segment are returned so the caller can
		// rewrite the full base chapter content (MergeAsync deletes by base path before saving).
		Assert.Equal(2, result.UpdatedSegments.Count);
		Assert.Same(baseSegment, result.UpdatedSegments[0]);
		Assert.Same(srcSegment, result.UpdatedSegments[1]);
		Assert.Equal(2, srcSegment.Order);
		Assert.StartsWith(baseChapter.Path + ".", srcSegment.Path);

		// Base segment keeps its canonical placement at the front
		Assert.Equal(1, baseSegment.Order);
		Assert.Equal($"{baseChapter.Path}.n{baseSegment.Id:N}", baseSegment.Path);
	}

	[Fact]
	public void Merge_MultipleSources_AppendsSequentially() {
		var baseChapter = new ChapterModel { Id = Guid.CreateVersion7(), Path = "n1.n2.n3", Order = 1, Title = "Base" };

		var src1 = new ChapterModel { Id = Guid.CreateVersion7(), Path = "n1.n2.n4", Order = 2, Title = "Src 1" };
		var seg1 = MakeTextSegment("A");
		seg1.Path = $"{src1.Path}.ns1";

		var src2 = new ChapterModel { Id = Guid.CreateVersion7(), Path = "n1.n2.n5", Order = 3, Title = "Src 2" };
		var seg2 = MakeTextSegment("B");
		seg2.Path = $"{src2.Path}.ns2";

		var result = ChapterMerger.Merge(
			baseChapter,
			[],
			[(src1, [seg1]), (src2, [seg2])]
		);

		Assert.Equal(2, result.UpdatedSegments.Count);
		Assert.Equal("A", ((TextSegmentModel)result.UpdatedSegments[0]).Runs.First().Text);
		Assert.Equal("B", ((TextSegmentModel)result.UpdatedSegments[1]).Runs.First().Text);
		Assert.Equal(1, result.UpdatedSegments[0].Order);
		Assert.Equal(2, result.UpdatedSegments[1].Order);
	}

	private static TextSegmentModel MakeTextSegment(string text) =>
		new() { Id = Guid.CreateVersion7(), Runs = [new TextRun(text)] };
}
