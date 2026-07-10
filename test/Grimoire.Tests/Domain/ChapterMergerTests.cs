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

		var updatedSegment = Assert.Single(result.UpdatedSegments);
		Assert.Same(srcSegment, updatedSegment);
		Assert.Equal(2, updatedSegment.Order);
		Assert.StartsWith(baseChapter.Path + ".", updatedSegment.Path);

		// Base segment remains unchanged
		Assert.Equal(1, baseSegment.Order);
		Assert.Equal($"{baseChapter.Path}.ns1", baseSegment.Path);
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
