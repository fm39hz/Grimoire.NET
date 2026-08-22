namespace Grimoire.Application.Service.Strategy;

using System.Text.RegularExpressions;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

/// <summary>
///     Parses Obsidian-flavored markdown into the Grimoire segment model.
///     Supported constructs map to dedicated segment types: paragraphs (with bold /
///     italic / strikethrough / highlight / inline-code runs and footnote refs),
///     thematic breaks, standalone images and GFM pipe tables. Every other block
///     (headings, fenced code, callouts, blockquotes, lists, raw HTML, wikilinks) is
///     preserved verbatim inside a single text segment so that a pull-push round trip
///     never loses data.
/// </summary>
public sealed partial class MarkdownSegmentParser {
	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
		.UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
		.Build();

	public sealed record ParsedDocument(List<SegmentModel> Segments);

	public ParsedDocument Parse(string markdown) {
		var (rawBody, footnotes, footnoteIdMap) = ExtractFootnoteDefinitions(markdown);

		// Replace footnote refs before parsing so Markdig does not interpret them as links.
		var body = FootnoteRefRegex().Replace(rawBody, static match => $"\uE000{match.Groups[1].Value}\uE001");

		var document = Markdown.Parse(body, Pipeline);
		var segments = new List<SegmentModel>();
		var order = 1.0;

		foreach (var block in document) {
			foreach (var segment in ParseBlock(block, body)) {
				segment.Order = order++;
				RewriteFootnoteIds(segment, footnoteIdMap);
				segments.Add(segment);
			}
		}

		foreach (var footnote in footnotes) {
			footnote.Order = order++;
			segments.Add(footnote);
		}

		return new ParsedDocument(segments);
	}

	[GeneratedRegex(@"(?m)^\[\^([^\s\]]+)\]:\s?(.*)$")]
	private static partial Regex FootnoteDefinitionRegex();

	[GeneratedRegex(@"==([^=\n]+?)==")]
	private static partial Regex HighlightRegex();

	[GeneratedRegex(@"\[\^([^\s\]]+)\]")]
	private static partial Regex FootnoteRefRegex();

	[GeneratedRegex("\uE000([^\uE001]*)\uE001")]
	private static partial Regex FootnoteSentinelRegex();

	private static (string Body, List<FootnoteSegmentModel> Footnotes, Dictionary<string, string> IdMap) ExtractFootnoteDefinitions(string markdown) {
		var footnotes = new List<FootnoteSegmentModel>();
		var idMap = new Dictionary<string, string>();
		var body = FootnoteDefinitionRegex().Replace(markdown, match => {
			var model = new FootnoteSegmentModel {
				Id = Guid.CreateVersion7(),
				Segments = [new TextSegmentModel {
					Id = Guid.CreateVersion7(),
					Runs = [new TextRun(match.Groups[2].Value.Trim())]
				}]
			};
			idMap[match.Groups[1].Value] = model.Id.ToString();
			footnotes.Add(model);
			return string.Empty;
		});

		return (body, footnotes, idMap);
	}

	private static void RewriteFootnoteIds(SegmentModel segment, Dictionary<string, string> idMap) {
		switch (segment) {
			case TextSegmentModel text:
				for (var i = 0; i < text.Runs.Count; i++) {
					var run = text.Runs[i];
					if (run.FootnoteId != null && idMap.TryGetValue(run.FootnoteId, out var guid)) {
						text.Runs[i] = run with { FootnoteId = guid };
					}
				}
				break;

			case TableSegmentModel table:
				RewriteTableCells(table.Header, idMap);
				foreach (var row in table.Rows) {
					RewriteTableCells(row, idMap);
				}
				break;
		}
	}

	private static void RewriteTableCells(List<TableCell> cells, Dictionary<string, string> idMap) {
		for (var i = 0; i < cells.Count; i++) {
			List<TextRun>? updated = null;
			for (var j = 0; j < cells[i].Runs.Count; j++) {
				var run = cells[i].Runs[j];
				if (run.FootnoteId != null && idMap.TryGetValue(run.FootnoteId, out var guid)) {
					updated ??= [.. cells[i].Runs];
					updated[j] = run with { FootnoteId = guid };
				}
			}

			if (updated is not null) {
				cells[i] = new TableCell(updated);
			}
		}
	}

	private static IEnumerable<SegmentModel> ParseBlock(Block block, string source) =>
		block switch {
			ParagraphBlock paragraph => IsPipeTable(SliceSource(block, source))
				? [BuildTable(SliceSource(block, source))]
				: ParseParagraph(paragraph, source),
			ThematicBreakBlock => [new DividerSegmentModel { Id = Guid.CreateVersion7() }],
			_ => [PreserveVerbatim(block, source)]
		};

	private static string SliceSource(Block block, string source) {
		var start = Math.Max(0, block.Span.Start);
		var end = Math.Min(source.Length, block.Span.End + 1);
		return source[start..end];
	}

	[GeneratedRegex(@"^\s*\|?\s*:?-{2,}:?\s*(\|\s*:?-{2,}:?\s*)*\|?\s*$")]
	private static partial Regex TableDelimiterRegex();

	private static bool IsPipeTable(string text) {
		var lines = text.Replace("\r\n", "\n").Split('\n');
		return lines.Length >= 2
			&& lines[0].Contains('|')
			&& TableDelimiterRegex().IsMatch(lines[1])
			&& lines[1].Contains('|');
	}

	private static TableSegmentModel BuildTable(string text) {
		var lines = text.Replace("\r\n", "\n").Split('\n');
		var model = new TableSegmentModel { Id = Guid.CreateVersion7() };

		for (var i = 0; i < lines.Length; i++) {
			if (i == 1) {
				continue; // delimiter row
			}

			var cells = SplitTableRow(lines[i]);
			if (i == 0) {
				model.Header.AddRange(cells.Select(static cellText => new TableCell(ParseCellRuns(cellText))));
			}
			else {
				model.Rows.Add(cells.Select(static cellText => new TableCell(ParseCellRuns(cellText))).ToList());
			}
		}

		if (model.Header.Count == 0) {
			model.Header.Add(new TableCell([]));
		}

		return model;
	}

	private static List<string> SplitTableRow(string line) {
		var trimmed = line.Trim();
		if (trimmed.StartsWith('|')) {
			trimmed = trimmed[1..];
		}
		if (trimmed.EndsWith('|')) {
			trimmed = trimmed[..^1];
		}
		return trimmed.Split('|').Select(static c => c.Trim()).ToList();
	}

	private static List<TextRun> ParseCellRuns(string cellText) {
		var runs = new List<TextRun>();
		if (Markdown.Parse(cellText, Pipeline).FirstOrDefault() is ParagraphBlock { Inline: not null } paragraph) {
			foreach (var inline in paragraph.Inline) {
				CollectInline(inline, new InlineFlags(), runs);
			}
		}
		return runs;
	}

	private static IEnumerable<SegmentModel> ParseParagraph(ParagraphBlock paragraph, string source) {
		if (paragraph.Inline is null || !paragraph.Inline.Any()) {
			yield break;
		}

		var inlines = paragraph.Inline.ToList();
		if (inlines.Count == 1 && inlines[0] is LinkInline { IsImage: true } image) {
			yield return new ImageSegmentModel {
				Id = Guid.CreateVersion7(),
				AssetKey = image.Url,
				Caption = GetLiteralText(image)
			};
			yield break;
		}

		var runs = new List<TextRun>();
		foreach (var inline in inlines) {
			CollectInline(inline, new InlineFlags(), runs);
		}

		if (runs.Count > 0) {
			yield return new TextSegmentModel { Id = Guid.CreateVersion7(), Runs = runs };
		}
	}

	private static SegmentModel PreserveVerbatim(Block block, string source) {
		var start = Math.Max(0, block.Span.Start);
		var end = Math.Min(source.Length, block.Span.End + 1);
		var text = source[start..end].TrimEnd();
		return new TextSegmentModel {
			Id = Guid.CreateVersion7(),
			Runs = [new TextRun(text)]
		};
	}

	private static void CollectInline(Inline inline, InlineFlags flags, List<TextRun> output) {
		switch (inline) {
			case LiteralInline literal:
				SplitLiteralText(literal.Content.ToString(), flags, output);
				break;

			case CodeInline code:
				output.Add(new TextRun(code.Content, IsCode: true));
				break;

			case EmphasisInline emphasis:
				var childFlags = flags.WithEmphasis(emphasis.DelimiterChar, emphasis.DelimiterCount);
				foreach (var child in emphasis) {
					CollectInline(child, childFlags, output);
				}
				break;

			case LinkInline link when link.IsImage:
				// Image nested among other content stays literal; a standalone image
				// paragraph is promoted to an ImageSegmentModel by ParseParagraph.
				output.Add(new TextRun($"![{GetLiteralText(link)}]({link.Url})"));
				break;

			case LineBreakInline:
				output.Add(new TextRun("\n"));
				break;

			case HtmlEntityInline entity:
				output.Add(new TextRun(entity.Original.ToString()));
				break;

			case HtmlInline html:
				output.Add(new TextRun(html.Tag));
				break;

			case ContainerInline container:
				foreach (var child in container) {
					CollectInline(child, flags, output);
				}
				break;

			default:
				var literalText = inline?.ToString();
				if (!string.IsNullOrEmpty(literalText)) {
					output.Add(new TextRun(literalText));
				}
				break;
		}
	}

	private static void SplitLiteralText(string text, InlineFlags flags, List<TextRun> output) {
		if (text.Length == 0) {
			return;
		}

		var cursor = 0;
		foreach (var match in HighlightRegex().Matches(text).Cast<Match>()) {
			if (match.Index > cursor) {
				SplitFootnoteRefs(text[cursor..match.Index], flags, output);
			}

			SplitFootnoteRefs(match.Groups[1].Value, flags with { IsHighlight = true }, output);
			cursor = match.Index + match.Length;
		}

		if (cursor < text.Length) {
			SplitFootnoteRefs(text[cursor..], flags, output);
		}
	}

	private static void SplitFootnoteRefs(string text, InlineFlags flags, List<TextRun> output) {
		if (text.Length == 0) {
			return;
		}

		var cursor = 0;
		foreach (var match in FootnoteSentinelRegex().Matches(text).Cast<Match>()) {
			if (match.Index > cursor) {
				output.Add(flags.ToRun(text[cursor..match.Index]));
			}

			output.Add(flags.ToRun(string.Empty) with { FootnoteId = match.Groups[1].Value });
			cursor = match.Index + match.Length;
		}

		if (cursor < text.Length) {
			output.Add(flags.ToRun(text[cursor..]));
		}
	}

	private static string GetLiteralText(ContainerInline container) {
		var sb = new System.Text.StringBuilder();
		foreach (var inline in container) {
			if (inline is LiteralInline literal) {
				sb.Append(literal.Content.ToString());
			}
			else if (inline is ContainerInline nested && nested is not LinkInline) {
				sb.Append(GetLiteralText(nested));
			}
		}
		return sb.ToString().Trim();
	}

	private readonly record struct InlineFlags(
		bool IsBold,
		bool IsItalic,
		bool IsStrikethrough,
		bool IsHighlight) {

		public TextRun ToRun(string text) => new(text, IsBold, IsItalic, null, IsStrikethrough, IsHighlight);

		public InlineFlags WithEmphasis(char delimiter, int count) => delimiter switch {
			'*' or '_' => count >= 2 ? this with { IsBold = true } : this with { IsItalic = true },
			'~' when count >= 2 => this with { IsStrikethrough = true },
			_ => this
		};
	}
}
