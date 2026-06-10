namespace Grimoire.Infrastructure.Export.Common;

using System;
using System.IO;
using Application.Dto.Book;
using Application.Export;

/// <summary>
///     Provides common utilities for export strategies
/// </summary>
public static class ExportUtilities {
	/// <summary>
	///     Sanitizes a filename by removing invalid characters
	/// </summary>
	public static string SanitizeFileName(string fileName) {
		var invalidChars = Path.GetInvalidFileNameChars();
		return string.Join("_", fileName.ToLowerInvariant().Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
	}

	/// <summary>
	///     Checks if the section option "splitDescription" is enabled
	/// </summary>
	public static bool IsSplitDescriptionEnabled(ExportSectionDto? section) =>
		section?.Options?.TryGetValue("splitDescription", out var splitVal) == true
		&& splitVal is bool split && split;

	/// <summary>
	///     Formats a footnote label based on style.
	/// </summary>
	public static string FormatFootnoteLabel(int number, FootnoteStyle style) => style switch {
		FootnoteStyle.Parentheses => $"({number})",
		FootnoteStyle.Asterisk => new string('*', number),
		FootnoteStyle.SuperScript => number.ToString(),
		_ => $"[{number}]"
	};

	/// <summary>
	///     Extracts dropcap parts from a text string.
	///     Returns true if a dropcap character was found, and populates the parts.
	/// </summary>
	public static bool TryExtractDropcapParts(string text, out string prefix, out char dropcapChar, out string suffix) {
		prefix = string.Empty;
		dropcapChar = default;
		suffix = string.Empty;

		if (string.IsNullOrEmpty(text)) {
			return false;
		}

		int letterIdx = 0;
		while (letterIdx < text.Length && !char.IsLetterOrDigit(text[letterIdx])) {
			letterIdx++;
		}

		if (letterIdx < text.Length) {
			prefix = text.Substring(0, letterIdx);
			dropcapChar = text[letterIdx];
			suffix = text.Substring(letterIdx + 1);
			return true;
		}

		return false;
	}
}
