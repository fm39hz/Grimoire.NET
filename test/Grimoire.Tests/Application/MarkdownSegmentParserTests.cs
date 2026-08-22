namespace Grimoire.Tests.Application;

using Grimoire.Application.Common;
using Grimoire.Application.Service.Strategy;
using Grimoire.Domain.Entity.Book;
using Grimoire.Domain.Entity.Book.Segment;
using Xunit;

public sealed class MarkdownSegmentParserTests {
	private readonly MarkdownSegmentParser parser = new();

	[Fact]
	public void Parse_PlainParagraphs_ProducesTextSegments() {
		var doc = parser.Parse("First paragraph.\n\nSecond paragraph.");

		var texts = WithoutFootnotes(doc.Segments).OfType<TextSegmentModel>().ToList();
		Assert.Equal(2, texts.Count);
		Assert.Equal("First paragraph.", texts[0].Runs.Single().Text);
		Assert.Equal("Second paragraph.", texts[1].Runs.Single().Text);
	}

	[Fact]
	public void Parse_SoftWrappedLines_StayInOneSegment() {
		var doc = parser.Parse("one two\nthree four");

		var text = Assert.Single(WithoutFootnotes(doc.Segments).OfType<TextSegmentModel>());
		Assert.Contains("one two", Rendered(text));
		Assert.Contains("three four", Rendered(text));
	}

	[Fact]
	public void Parse_BoldItalicStrikeCodeHighlight_MapsToRunFlags() {
		var doc = parser.Parse("**bold** *it* ~~gone~~ `code` ==mark==");

		var text = Assert.Single(WithoutFootnotes(doc.Segments).OfType<TextSegmentModel>());
		Assert.Contains(text.Runs, r => r.Text == "bold" && r.IsBold);
		Assert.Contains(text.Runs, r => r.Text == "it" && r.IsItalic);
		Assert.Contains(text.Runs, r => r.Text == "gone" && r.IsStrikethrough);
		Assert.Contains(text.Runs, r => r.Text == "code" && r.IsCode);
		Assert.Contains(text.Runs, r => r.Text == "mark" && r.IsHighlight);
	}

	// ── footnotes ─────────────────────────────────────────────────────────────

	[Fact]
	public void Parse_FootnoteRefAndDefinition_LinkViaIds() {
		var doc = parser.Parse("Body text[^1] here.\n\n[^1]: The note.");

		var footnote = Assert.Single(doc.Segments.OfType<FootnoteSegmentModel>());
		var body = doc.Segments.OfType<TextSegmentModel>().Single(s => s.Runs.Any(r => r.FootnoteId != null));
		var refRun = body.Runs.Single(r => r.FootnoteId != null);

		Assert.Equal(footnote.Id.ToString(), refRun.FootnoteId);
		Assert.Equal("The note.", footnote.Segments.Single().Runs.Single().Text);
	}

	// ── divider ───────────────────────────────────────────────────────────────

	[Fact]
	public void Parse_ThematicBreak_ProducesDivider() {
		var doc = parser.Parse("Before.\n\n---\n\nAfter.");

		Assert.Single(doc.Segments.OfType<DividerSegmentModel>());
		Assert.Equal(2, WithoutFootnotes(doc.Segments).OfType<TextSegmentModel>().Count());
	}

	// ── image ─────────────────────────────────────────────────────────────────

	[Fact]
	public void Parse_StandaloneImage_ProducesImageSegment() {
		const string assetKey = "ast_0123456789abcdef0123456789abcdef";
		var doc = parser.Parse($"Intro\n\n![Alt text]({assetKey})\n");

		var image = Assert.Single(doc.Segments.OfType<ImageSegmentModel>());
		Assert.Equal(assetKey, image.AssetKey);
		Assert.Equal("Alt text", image.Caption);
	}

	// ── table ─────────────────────────────────────────────────────────────────

	[Fact]
	public void Parse_PipeTable_ProducesTableSegment() {
		var doc = parser.Parse("| A | B |\n| --- | --- |\n| 1 | 2 |\n| **3** | 4 |");

		var table = Assert.Single(doc.Segments.OfType<TableSegmentModel>());
		Assert.Equal(2, table.Header.Count);
		Assert.Equal(2, table.Rows.Count);
		Assert.Equal(2, table.Rows[0].Count);
		Assert.Contains(table.Rows[1][0].Runs, r => r.IsBold);
	}

	[Fact]
	public void Parse_TableWithBoldHeader_CarriesFormatting() {
		var doc = parser.Parse("| **H1** | H2 |\n| --- | --- |\n| a | b |");

		var table = doc.Segments.OfType<TableSegmentModel>().Single();
		Assert.Contains(table.Header[0].Runs, r => r.IsBold && r.Text == "H1");
	}

	// ── verbatim preservation ─────────────────────────────────────────────────

	[Fact]
	public void Parse_FencedCodeBlock_PreservedVerbatim() {
		const string markdown = "```\nvar x = 1;\n```";
		var doc = parser.Parse(markdown);

		var text = Assert.Single(doc.Segments.OfType<TextSegmentModel>());
		Assert.Contains("```", Rendered(text));
		Assert.Contains("var x = 1;", Rendered(text));
	}

	[Fact]
	public void Parse_AtxHeading_PreservedVerbatim() {
		var doc = parser.Parse("# Heading line");

		var text = Assert.Single(doc.Segments.OfType<TextSegmentModel>());
		Assert.StartsWith("#", Rendered(text));
	}

	[Fact]
	public void Parse_RawHtmlBlock_DoesNotRejectContent() {
		var doc = parser.Parse("<div>html block</div>");

		var text = Assert.Single(doc.Segments.OfType<TextSegmentModel>());
		Assert.Contains("<div>", Rendered(text));
	}

	// ── fixed point: render(parse(render(parse(x)))) inventory stable ────────

	[Fact]
	public void RoundTrip_SecondCycleIsStable() {
		const string markdown = "Para one.\n\n**Bold** and *it*.\n\n---\n\n| A | B |\n| - | - |\n| 1 | 2 |\n";

		var firstPass = SegmentMarkdownConverter.ConvertToMarkdown(parser.Parse(markdown).Segments);
		var secondPass = SegmentMarkdownConverter.ConvertToMarkdown(parser.Parse(firstPass).Segments);

		AssertInventoryEqual(parser.Parse(markdown).Segments, parser.Parse(secondPass).Segments);
	}

	private static void AssertInventoryEqual(IReadOnlyList<SegmentModel> expected, IReadOnlyList<SegmentModel> actual) {
		Assert.Equal(expected.OfType<TextSegmentModel>().Count(), actual.OfType<TextSegmentModel>().Count());
		Assert.Equal(expected.OfType<DividerSegmentModel>().Count(), actual.OfType<DividerSegmentModel>().Count());
		Assert.Equal(expected.OfType<ImageSegmentModel>().Count(), actual.OfType<ImageSegmentModel>().Count());
		Assert.Equal(expected.OfType<FootnoteSegmentModel>().Count(), actual.OfType<FootnoteSegmentModel>().Count());

		var expectedTables = expected.OfType<TableSegmentModel>().ToList();
		var actualTables = actual.OfType<TableSegmentModel>().ToList();
		Assert.Equal(expectedTables.Count, actualTables.Count);
		for (var i = 0; i < expectedTables.Count; i++) {
			Assert.Equal(expectedTables[i].Header.Count, actualTables[i].Header.Count);
			Assert.Equal(expectedTables[i].Rows.Count, actualTables[i].Rows.Count);
		}
	}

	private static string Rendered(TextSegmentModel segment) =>
		string.Concat(segment.Runs.Select(r => r.Text));

	private static List<SegmentModel> WithoutFootnotes(IReadOnlyList<SegmentModel> segments) =>
		segments.Where(s => s is not FootnoteSegmentModel).ToList();
}
