namespace Grimoire.Tests.Application;

using System.Linq;
using Grimoire.Application.Common;
using Grimoire.Application.Dto.Book;
using Grimoire.Domain.Entity.Book;
using Grimoire.Domain.Entity.Book.Segment;
using Xunit;

public sealed class FootnoteRemapperTests {
	// ── null / empty footnotes ────────────────────────────────────────────────

	[Fact]
	public void Remap_NullFootnotes_SegmentsPassedThrough() {
		var seg = MakeTextSeg("hello", footnoteId: null);
		var result = FootnoteRemapper.Remap([seg], null);

		var mapped = Assert.Single(result.Segments);
		var text = Assert.IsType<TextSegmentModel>(mapped);
		Assert.Null(text.Runs.First().FootnoteId);
	}

	[Fact]
	public void Remap_EmptyFootnotes_NoRunsModified() {
		var seg = MakeTextSeg("hello", footnoteId: null);
		var result = FootnoteRemapper.Remap([seg], []);
		Assert.Single(result.Segments);
		Assert.Empty(result.Footnotes);
	}

	// ── footnote id remapping ─────────────────────────────────────────────────

	[Fact]
	public void Remap_RunFootnoteIdRemappedToSystemGuid() {
		const string initialId = "fn-1";
		var seg = MakeTextSeg("text", footnoteId: initialId);
		var footnote = new ImportFootnoteDto { InitialId = initialId };

		var result = FootnoteRemapper.Remap([seg], [footnote]);

		var text = Assert.IsType<TextSegmentModel>(Assert.Single(result.Segments));
		var newId = text.Runs.First().FootnoteId;
		Assert.NotNull(newId);
		Assert.True(Guid.TryParse(newId, out _), $"FootnoteId should be a GUID but was: {newId}");
		Assert.NotEqual(initialId, newId);
	}

	[Fact]
	public void Remap_FootnoteSystemIdMatchesRunFootnoteId() {
		const string initialId = "fn-A";
		var seg = MakeTextSeg("text", footnoteId: initialId);
		var footnote = new ImportFootnoteDto { InitialId = initialId };

		var result = FootnoteRemapper.Remap([seg], [footnote]);

		var text = Assert.IsType<TextSegmentModel>(result.Segments[0]);
		var runId = text.Runs.First().FootnoteId!;
		var systemNote = Assert.Single(result.Footnotes);
		Assert.Equal(systemNote.Id.ToString(), runId);
	}

	[Fact]
	public void Remap_MultipleFootnotes_EachRunGetsCorrectSystemId() {
		const string idA = "A";
		const string idB = "B";
		var seg = MakeTextSeg("text", footnoteId: idA);
		var seg2 = MakeTextSeg("text2", footnoteId: idB);

		var result = FootnoteRemapper.Remap([seg, seg2], [
			new ImportFootnoteDto { InitialId = idA },
			new ImportFootnoteDto { InitialId = idB }
		]);

		Assert.Equal(2, result.Footnotes.Count);
		var runIdA = ((TextSegmentModel)result.Segments[0]).Runs.First().FootnoteId!;
		var runIdB = ((TextSegmentModel)result.Segments[1]).Runs.First().FootnoteId!;
		Assert.NotEqual(runIdA, runIdB);
	}

	// ── guard: invalid footnote entry ─────────────────────────────────────────

	[Fact]
	public void Remap_FootnoteWithNullInitialId_IsSkipped() {
		var seg = MakeTextSeg("text", footnoteId: null);
		var footnote = new ImportFootnoteDto { InitialId = null };

		var result = FootnoteRemapper.Remap([seg], [footnote]);

		Assert.Empty(result.Footnotes);
	}

	// ── run without mapping ───────────────────────────────────────────────────

	[Fact]
	public void Remap_RunFootnoteIdNotInMap_IdKeptUnchanged() {
		const string unmappedId = "not-in-map";
		var seg = MakeTextSeg("text", footnoteId: unmappedId);

		var result = FootnoteRemapper.Remap([seg], []);

		var text = Assert.IsType<TextSegmentModel>(result.Segments[0]);
		Assert.Equal(unmappedId, text.Runs.First().FootnoteId);
	}

	// ── non-text segments ─────────────────────────────────────────────────────

	[Fact]
	public void Remap_ImageSegments_PassedThroughUnchanged() {
		var img = new ImageSegmentModel { Id = Guid.CreateVersion7(), AssetKey = "ast_abc" };
		var result = FootnoteRemapper.Remap([img], null);
		var mapped = Assert.IsType<ImageSegmentModel>(Assert.Single(result.Segments));
		Assert.Equal("ast_abc", mapped.AssetKey);
	}

	// ── ExtractReferencedIds ──────────────────────────────────────────────────

	[Fact]
	public void ExtractReferencedIds_ReturnsAllReferenced() {
		var seg = MakeTextSeg("t", "fn-1");
		var seg2 = MakeTextSeg("t", "fn-2");
		var ids = FootnoteRemapper.ExtractReferencedIds([seg, seg2]);
		Assert.Contains("fn-1", ids);
		Assert.Contains("fn-2", ids);
	}

	[Fact]
	public void ExtractReferencedIds_NoFootnoteRefs_ReturnsEmpty() {
		var ids = FootnoteRemapper.ExtractReferencedIds([MakeTextSeg("plain")]);
		Assert.Empty(ids);
	}

	[Fact]
	public void ExtractReferencedIds_NonTextSegmentsIgnored() {
		var img = new ImageSegmentModel { Id = Guid.CreateVersion7(), AssetKey = "k" };
		var ids = FootnoteRemapper.ExtractReferencedIds([img]);
		Assert.Empty(ids);
	}

	// ── helpers ────────────────────────────────────────────────────────────────

	private static TextSegmentModel MakeTextSeg(string text, string? footnoteId = null) =>
		new() {
			Id = Guid.CreateVersion7(),
			Runs = [new TextRun(text) { FootnoteId = footnoteId }]
		};
}
