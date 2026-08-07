namespace Grimoire.Application.Common;

using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;
using Mapper;

public static class FootnoteRemapper {
	public record Result(
		List<SegmentModel> Segments,
		List<FootnoteSegmentModel> Footnotes);

	public static Result Remap(
		IEnumerable<SegmentModel> rawSegments,
		IEnumerable<ImportFootnoteDto>? rawFootnotes) {
		var idMap = new Dictionary<string, Guid>();
		var cleanFootnotes = new List<FootnoteSegmentModel>();

		foreach (var note in rawFootnotes ?? []) {
			if (note is null || string.IsNullOrEmpty(note.InitialId)) {
				continue;
			}

			var systemId = Guid.CreateVersion7();
			idMap[note.InitialId] = systemId;
			cleanFootnotes.Add(new FootnoteSegmentModel {
				Id = systemId,
				Segments = [.. note.Segments.Select(static s => new TextSegmentModel {
					Id = Guid.TryParse(s.Id, out var g) ? g : Guid.CreateVersion7(),
					Runs = [.. s.Runs.Select(static r => new TextRun(r.Text, r.IsBold, r.IsItalic, r.FootnoteId))]
				})]
			});
		}

		var cleanContent = new List<SegmentModel>();
		foreach (var segment in rawSegments) {
			if (segment is TextSegmentModel textSeg) {
				var updatedRuns = textSeg.Runs.Select(run =>
					!string.IsNullOrEmpty(run.FootnoteId) && idMap.TryGetValue(run.FootnoteId, out var sid)
						? run with { FootnoteId = sid.ToString() }
						: run
				).ToList();

				cleanContent.Add(new TextSegmentModel {
					Id = textSeg.Id,
					Runs = updatedRuns
				});
			}
			else {
				cleanContent.Add(segment);
			}
		}

		return new Result(cleanContent, cleanFootnotes);
	}

	public static HashSet<string> ExtractReferencedIds(IEnumerable<SegmentModel> segments) {
		var footnoteIds = new HashSet<string>();

		foreach (var segment in segments) {
			if (segment is not TextSegmentModel textSegment) {
				continue;
			}

			foreach (var run in textSegment.Runs) {
				if (!string.IsNullOrEmpty(run.FootnoteId)) {
					footnoteIds.Add(run.FootnoteId);
				}
			}
		}

		return footnoteIds;
	}
}
