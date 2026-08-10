namespace Grimoire.Application.Ingestion.Reconciliation;

using System.Globalization;
using System.Text;

public static class TitleNormalizer {
	public static string Normalize(string? value) {
		if (string.IsNullOrWhiteSpace(value)) return string.Empty;

		var decomposed = value.Normalize(NormalizationForm.FormD);
		var result = new StringBuilder(decomposed.Length);
		var pendingSpace = false;
		foreach (var rune in decomposed.EnumerateRunes()) {
			var category = Rune.GetUnicodeCategory(rune);
			if (category == UnicodeCategory.NonSpacingMark) continue;

			if (Rune.IsLetterOrDigit(rune)) {
				if (pendingSpace && result.Length > 0) result.Append(' ');
				foreach (var lowered in rune.ToString().ToLowerInvariant()) result.Append(lowered);
				pendingSpace = false;
			}
			else {
				pendingSpace = true;
			}
		}

		return result.ToString();
	}
}
