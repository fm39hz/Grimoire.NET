namespace Grimoire.Tests.Application;

using Grimoire.Application.Common;
using Grimoire.Domain.Entity.Book;
using Grimoire.Domain.Entity.Book.Segment;
using Xunit;

public sealed class SegmentMarkdownConverterTests {
	// ── empty input ───────────────────────────────────────────────────────────

	[Fact]
	public void ConvertToMarkdown_EmptySegments_ReturnsEmptyString() {
		var result = SegmentMarkdownConverter.ConvertToMarkdown(new List<SegmentModel>());
		Assert.Equal(string.Empty, result);
	}

	// ── plain text ────────────────────────────────────────────────────────────

	[Fact]
	public void ConvertToMarkdown_PlainText_NoMarkup() {
		var seg = MakeSeg("Hello world");
		var result = SegmentMarkdownConverter.ConvertToMarkdown([seg]);
		Assert.Contains("Hello world", result);
		Assert.DoesNotContain("*", result);
		Assert.DoesNotContain("**", result);
	}

	// ── formatting ────────────────────────────────────────────────────────────

	[Fact]
	public void ConvertToMarkdown_BoldRun_WrappedInDoubleAsterisk() {
		var seg = MakeSegWithRun("bold text", isBold: true);
		var result = SegmentMarkdownConverter.ConvertToMarkdown([seg]);
		Assert.Contains("**bold text**", result);
	}

	[Fact]
	public void ConvertToMarkdown_ItalicRun_WrappedInSingleAsterisk() {
		var seg = MakeSegWithRun("italic text", isItalic: true);
		var result = SegmentMarkdownConverter.ConvertToMarkdown([seg]);
		Assert.Contains("*italic text*", result);
	}

	[Fact]
	public void ConvertToMarkdown_BoldAndItalicRun_WrappedInTripleAsterisk() {
		var seg = MakeSegWithRun("bold italic", isBold: true, isItalic: true);
		var result = SegmentMarkdownConverter.ConvertToMarkdown([seg]);
		Assert.Contains("***bold italic***", result);
	}

	// ── image segment ─────────────────────────────────────────────────────────

	[Fact]
	public void ConvertToMarkdown_ImageSegment_MarkdownImageSyntax() {
		var img = new ImageSegmentModel {
			Id = Guid.CreateVersion7(),
			AssetKey = "ast_123",
			Caption = "A caption"
		};
		var result = SegmentMarkdownConverter.ConvertToMarkdown([img]);
		Assert.Contains("![A caption](ast_123)", result);
	}

	[Fact]
	public void ConvertToMarkdown_ImageSegmentNullCaption_DefaultsToImage() {
		var img = new ImageSegmentModel {
			Id = Guid.CreateVersion7(),
			AssetKey = "ast_456",
			Caption = null
		};
		var result = SegmentMarkdownConverter.ConvertToMarkdown([img]);
		Assert.Contains("![Image](ast_456)", result);
	}

	// ── divider segment ───────────────────────────────────────────────────────

	[Fact]
	public void ConvertToMarkdown_DividerSegment_OutputsStyle() {
		var divider = new DividerSegmentModel { Id = Guid.CreateVersion7(), Style = "---" };
		var result = SegmentMarkdownConverter.ConvertToMarkdown([divider]);
		Assert.Contains("---", result);
	}

	// ── footnote references in runs ───────────────────────────────────────────

	[Fact]
	public void ConvertToMarkdown_RunWithFootnoteAndText_AppendsBracketRef() {
		var noteId = Guid.CreateVersion7();
		var note = new FootnoteSegmentModel {
			Id = noteId,
			Segments = [MakeSeg("Note content")]
		};
		var run = new TextRun("see here", FootnoteId: noteId.ToString());
		var seg = new TextSegmentModel { Id = Guid.CreateVersion7(), Runs = [run] };

		var result = SegmentMarkdownConverter.ConvertToMarkdown([seg], [note]);

		Assert.Contains("see here[^1]", result);
	}

	[Fact]
	public void ConvertToMarkdown_RunWithEmptyTextAndFootnote_OnlyOutputsBracketRef() {
		var noteId = Guid.CreateVersion7();
		var note = new FootnoteSegmentModel { Id = noteId, Segments = [MakeSeg("Note")] };
		var run = new TextRun("", FootnoteId: noteId.ToString());
		var seg = new TextSegmentModel { Id = Guid.CreateVersion7(), Runs = [run] };

		var result = SegmentMarkdownConverter.ConvertToMarkdown([seg], [note]);

		Assert.Contains("[^1]", result);
		Assert.DoesNotContain("[^1][^1]", result);
	}

	// ── footnote definitions at end ───────────────────────────────────────────

	[Fact]
	public void ConvertToMarkdown_FootnoteDefinitions_AppendedAtEnd() {
		var noteId = Guid.CreateVersion7();
		var note = new FootnoteSegmentModel {
			Id = noteId,
			Segments = [MakeSeg("Footnote text")]
		};
		var run = new TextRun("ref", FootnoteId: noteId.ToString());
		var seg = new TextSegmentModel { Id = Guid.CreateVersion7(), Runs = [run] };

		var result = SegmentMarkdownConverter.ConvertToMarkdown([seg], [note]);

		Assert.Contains("[^1]: Footnote text", result);
		// definition must come after the content
		var contentIndex = result.IndexOf("ref", StringComparison.Ordinal);
		var defIndex = result.IndexOf("[^1]:", StringComparison.Ordinal);
		Assert.True(defIndex > contentIndex, "Footnote definition should appear after content");
	}

	// ── multiple segments separated by blank lines ────────────────────────────

	[Fact]
	public void ConvertToMarkdown_MultipleSegments_SeparatedByBlankLine() {
		var seg1 = MakeSeg("Para one");
		var seg2 = MakeSeg("Para two");
		var result = SegmentMarkdownConverter.ConvertToMarkdown([seg1, seg2]);
		// blank line between means two newlines separating
		Assert.Contains("Para one", result);
		Assert.Contains("Para two", result);
		var idx1 = result.IndexOf("Para one", StringComparison.Ordinal);
		var idx2 = result.IndexOf("Para two", StringComparison.Ordinal);
		var between = result[idx1..idx2];
		Assert.Contains("\n\n", between);
	}

	// ── helpers ────────────────────────────────────────────────────────────────

	private static TextSegmentModel MakeSeg(string text) =>
		new() { Id = Guid.CreateVersion7(), Runs = [new TextRun(text)] };

	private static TextSegmentModel MakeSegWithRun(string text, bool isBold = false, bool isItalic = false) =>
		new() { Id = Guid.CreateVersion7(), Runs = [new TextRun(text, IsBold: isBold, IsItalic: isItalic)] };
}
