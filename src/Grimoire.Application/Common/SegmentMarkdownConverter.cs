namespace Grimoire.Application.Common;

using System.Text;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book.Segment;

/// <summary>
///     Utility class for converting segment data to Markdown format.
///     Supports both domain models and DTOs.
/// </summary>
public static class SegmentMarkdownConverter {
	#region DTO-based methods (for API layer)

	/// <summary>
	///     Converts a list of segment DTOs and footnotes to Markdown format.
	/// </summary>
	/// <param name="segments">The content segments to convert.</param>
	/// <param name="footnotes">The footnote definitions.</param>
	/// <returns>A Markdown-formatted string representation of the content.</returns>
	public static string ConvertToMarkdown(List<SegmentDto> segments, List<FootnoteSegmentDto>? footnotes = null) {
		if (segments.Count == 0) {
			return string.Empty;
		}

		var sb = new StringBuilder();
		var footnoteMap = BuildFootnoteMapFromDto(footnotes);

		foreach (var segment in segments) {
			var markdown = ConvertSegmentDtoToMarkdown(segment, footnoteMap);
			if (!string.IsNullOrWhiteSpace(markdown)) {
				sb.AppendLine(markdown);
				sb.AppendLine(); // Add blank line between paragraphs
			}
		}

		// Append footnote definitions at the end
		if (footnoteMap.Count > 0 && footnotes is not null) {
			AppendFootnoteDefinitionsFromDto(sb, footnotes, footnoteMap);
		}

		return sb.ToString().TrimEnd();
	}

	/// <summary>
	///     Converts a list of text segment DTOs to Markdown format (e.g., for series description).
	/// </summary>
	/// <param name="textSegments">The text segments to convert.</param>
	/// <returns>A Markdown-formatted string representation of the text content.</returns>
	public static string ConvertTextSegmentsToMarkdown(List<TextSegmentDto> textSegments) {
		if (textSegments.Count == 0) {
			return string.Empty;
		}

		var sb = new StringBuilder();

		foreach (var segment in textSegments) {
			var markdown = ConvertTextSegmentDtoToMarkdown(segment);
			if (!string.IsNullOrWhiteSpace(markdown)) {
				sb.AppendLine(markdown);
				sb.AppendLine(); // Add blank line between paragraphs
			}
		}

		return sb.ToString().TrimEnd();
	}

	#endregion

	#region Domain model-based methods (for Export strategies)

	/// <summary>
	///     Converts a list of domain segment models and footnotes to Markdown format.
	/// </summary>
	/// <param name="segments">The content segments to convert.</param>
	/// <param name="footnotes">The footnote definitions.</param>
	/// <returns>A Markdown-formatted string representation of the content.</returns>
	public static string ConvertToMarkdown(List<SegmentModel> segments, List<FootnoteSegmentModel>? footnotes = null) {
		if (segments.Count == 0) {
			return string.Empty;
		}

		var sb = new StringBuilder();
		var footnoteMap = BuildFootnoteMapFromModel(footnotes);

		foreach (var segment in segments) {
			var markdown = ConvertSegmentModelToMarkdown(segment, footnoteMap);
			if (!string.IsNullOrWhiteSpace(markdown)) {
				sb.AppendLine(markdown);
				sb.AppendLine(); // Add blank line between paragraphs
			}
		}

		// Append footnote definitions at the end
		if (footnoteMap.Count > 0 && footnotes is not null) {
			AppendFootnoteDefinitionsFromModel(sb, footnotes, footnoteMap);
		}

		return sb.ToString().TrimEnd();
	}

	/// <summary>
	///     Converts a list of text segment models to Markdown format (e.g., for series description).
	/// </summary>
	/// <param name="textSegments">The text segments to convert.</param>
	/// <returns>A Markdown-formatted string representation of the text content.</returns>
	public static string ConvertTextSegmentsToMarkdown(List<TextSegmentModel> textSegments) {
		if (textSegments.Count == 0) {
			return string.Empty;
		}

		var sb = new StringBuilder();

		foreach (var segment in textSegments) {
			var markdown = ConvertTextSegmentModelToMarkdown(segment);
			if (!string.IsNullOrWhiteSpace(markdown)) {
				sb.AppendLine(markdown);
				sb.AppendLine(); // Add blank line between paragraphs
			}
		}

		return sb.ToString().TrimEnd();
	}

	#endregion

	#region DTO conversion helpers

	private static Dictionary<string, int> BuildFootnoteMapFromDto(List<FootnoteSegmentDto>? footnotes) {
		var map = new Dictionary<string, int>();
		if (footnotes is null || footnotes.Count == 0) {
			return map;
		}

		for (var i = 0; i < footnotes.Count; i++) {
			var footnoteId = footnotes[i].Id;
			var key = footnoteId.StartsWith("seg_") ? footnoteId[4..] : footnoteId;
			map[key] = i + 1;
		}

		return map;
	}

	private static string ConvertSegmentDtoToMarkdown(SegmentDto segment, Dictionary<string, int> footnoteMap) => segment switch {
		TextSegmentDto textSegment => ConvertTextSegmentDtoToMarkdown(textSegment, footnoteMap),
		ImageSegmentDto imageSegment => ConvertImageSegmentDtoToMarkdown(imageSegment),
		DividerSegmentDto dividerSegment => ConvertDividerSegmentDtoToMarkdown(dividerSegment),
		_ => string.Empty
	};

	private static string ConvertTextSegmentDtoToMarkdown(TextSegmentDto segment, Dictionary<string, int>? footnoteMap = null) {
		if (segment.Runs.Count == 0) {
			return string.Empty;
		}

		var sb = new StringBuilder();

		foreach (var run in segment.Runs) {
			var text = run.Text;

			// Check if this run has a footnote reference
			var hasFootnote = !string.IsNullOrEmpty(run.FootnoteId) &&
							  footnoteMap is not null &&
							  footnoteMap.TryGetValue(run.FootnoteId, out var footnoteIndex);

			// If the run is just a footnote marker (empty text with footnoteId), output only the reference
			if (string.IsNullOrEmpty(text) && hasFootnote) {
				sb.Append($"[^{footnoteMap![run.FootnoteId!]}]");
				continue;
			}

			// Add footnote reference inside the text (before formatting closes)
			if (hasFootnote) {
				text += $"[^{footnoteMap![run.FootnoteId!]}]";
			}

			// Apply formatting around text (including footnote reference)
			if (run.IsBold && run.IsItalic) {
				text = $"***{text}***";
			}
			else if (run.IsBold) {
				text = $"**{text}**";
			}
			else if (run.IsItalic) {
				text = $"*{text}*";
			}

			sb.Append(text);
		}

		return sb.ToString();
	}

	private static string ConvertImageSegmentDtoToMarkdown(ImageSegmentDto segment) {
		var altText = segment.Caption ?? "Image";
		return $"![{altText}]({segment.AssetKey})";
	}

	private static string ConvertDividerSegmentDtoToMarkdown(DividerSegmentDto segment) => segment.Style;

	private static void AppendFootnoteDefinitionsFromDto(StringBuilder sb, List<FootnoteSegmentDto> footnotes, Dictionary<string, int> footnoteMap) {
		sb.AppendLine(); // Extra blank line before footnotes

		foreach (var footnote in footnotes) {
			var footnoteId = footnote.Id;
			var key = footnoteId.StartsWith("seg_") ? footnoteId[4..] : footnoteId;
			if (!footnoteMap.TryGetValue(key, out var index)) {
				continue;
			}

			var content = ConvertFootnoteContentFromDto(footnote.Segments);
			sb.AppendLine($"[^{index}]: {content}");
		}
	}

	private static string ConvertFootnoteContentFromDto(List<TextSegmentDto> segments) {
		if (segments.Count == 0) {
			return string.Empty;
		}

		var parts = new List<string>();
		foreach (var segment in segments) {
			var text = ConvertTextSegmentDtoToMarkdown(segment);
			if (!string.IsNullOrWhiteSpace(text)) {
				parts.Add(text);
			}
		}

		return string.Join(" ", parts);
	}

	#endregion

	#region Domain model conversion helpers

	private static Dictionary<string, int> BuildFootnoteMapFromModel(List<FootnoteSegmentModel>? footnotes) {
		var map = new Dictionary<string, int>();
		if (footnotes is null || footnotes.Count == 0) {
			return map;
		}

		for (var i = 0; i < footnotes.Count; i++) {
			map[footnotes[i].Id.ToString()] = i + 1;
		}

		return map;
	}

	private static string ConvertSegmentModelToMarkdown(SegmentModel segment, Dictionary<string, int> footnoteMap) => segment switch {
		TextSegmentModel textSegment => ConvertTextSegmentModelToMarkdown(textSegment, footnoteMap),
		ImageSegmentModel imageSegment => ConvertImageSegmentModelToMarkdown(imageSegment),
		DividerSegmentModel dividerSegment => ConvertDividerSegmentModelToMarkdown(dividerSegment),
		_ => string.Empty
	};

	private static string ConvertTextSegmentModelToMarkdown(TextSegmentModel segment, Dictionary<string, int>? footnoteMap = null) {
		if (segment.Runs.Count == 0) {
			return string.Empty;
		}

		var sb = new StringBuilder();

		foreach (var run in segment.Runs) {
			var text = run.Text;

			// Check if this run has a footnote reference
			var hasFootnote = !string.IsNullOrEmpty(run.FootnoteId) &&
							  footnoteMap is not null &&
							  footnoteMap.TryGetValue(run.FootnoteId, out var footnoteIndex);

			// If the run is just a footnote marker (empty text with footnoteId), output only the reference
			if (string.IsNullOrEmpty(text) && hasFootnote) {
				sb.Append($"[^{footnoteMap![run.FootnoteId!]}]");
				continue;
			}

			// Add footnote reference inside the text (before formatting closes)
			if (hasFootnote) {
				text += $"[^{footnoteMap![run.FootnoteId!]}]";
			}

			// Apply formatting around text (including footnote reference)
			if (run.IsBold && run.IsItalic) {
				text = $"***{text}***";
			}
			else if (run.IsBold) {
				text = $"**{text}**";
			}
			else if (run.IsItalic) {
				text = $"*{text}*";
			}

			sb.Append(text);
		}

		return sb.ToString();
	}

	private static string ConvertImageSegmentModelToMarkdown(ImageSegmentModel segment) {
		var altText = segment.Caption ?? "Image";
		return $"![{altText}]({segment.AssetKey})";
	}

	private static string ConvertDividerSegmentModelToMarkdown(DividerSegmentModel segment) => segment.Style;

	private static void AppendFootnoteDefinitionsFromModel(StringBuilder sb, List<FootnoteSegmentModel> footnotes, Dictionary<string, int> footnoteMap) {
		sb.AppendLine(); // Extra blank line before footnotes

		foreach (var footnote in footnotes) {
			if (!footnoteMap.TryGetValue(footnote.Id.ToString(), out var index)) {
				continue;
			}

			var content = ConvertFootnoteContentFromModel(footnote.Segments);
			sb.AppendLine($"[^{index}]: {content}");
		}
	}

	private static string ConvertFootnoteContentFromModel(List<TextSegmentModel> segments) {
		if (segments.Count == 0) {
			return string.Empty;
		}

		var parts = new List<string>();
		foreach (var segment in segments) {
			var text = ConvertTextSegmentModelToMarkdown(segment);
			if (!string.IsNullOrWhiteSpace(text)) {
				parts.Add(text);
			}
		}

		return string.Join(" ", parts);
	}

	#endregion
}
