namespace Grimoire.Application.Ingestion.Reconciliation;

using System.Security.Cryptography;
using System.Text;
using Domain.Entity.Book.Segment;
using Domain.Entity.Book;

public static class BookContentFingerprint {
	public static string Compute(IEnumerable<SegmentModel> segments) {
		var builder = new StringBuilder();
		foreach (var segment in segments.OrderBy(static segment => segment.Order)) {
			builder.Append(segment switch {
				TextSegmentModel => "T|",
				ImageSegmentModel => "I|",
				DividerSegmentModel => "D|",
				FootnoteSegmentModel => "F|",
				_ => "?|"
			});
			switch (segment) {
				case TextSegmentModel text:
					AppendRuns(builder, text.Runs);
					break;
				case ImageSegmentModel image:
					Append(builder, image.AssetKey);
					Append(builder, image.Caption);
					break;
				case DividerSegmentModel divider:
					Append(builder, divider.Style);
					break;
				case FootnoteSegmentModel footnote:
					foreach (var nested in footnote.Segments) AppendRuns(builder, nested.Runs);
					break;
			}
		}
		return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
	}

	private static void AppendRuns(StringBuilder builder, IEnumerable<TextRun> runs) {
		foreach (var run in runs) {
			Append(builder, run.Text);
			builder.Append(run.IsBold ? '1' : '0').Append(run.IsItalic ? '1' : '0');
			Append(builder, run.FootnoteId);
		}
	}

	private static void Append(StringBuilder builder, string? value) =>
		builder.Append(value?.Length ?? -1).Append(':').Append(value).Append('|');
}
