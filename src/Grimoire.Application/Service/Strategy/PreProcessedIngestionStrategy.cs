namespace Grimoire.Application.Service.Strategy;

using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;

/// <summary>
///     Strategy for ingesting pre-processed content (Content is already segmented)
/// </summary>
public class PreProcessedIngestionStrategy : IIngestionStrategy {
	public bool CanHandle(CreateChapterRequestDto dto) =>
		// Can handle if Content array exists (even if empty)
		dto.Content is not null;

	public Task<IngestionResult> ExecuteAsync(CreateChapterRequestDto dto, Guid volumeId) {
		if (!CanHandle(dto)) {
			throw new InvalidOperationException("This strategy cannot handle the provided DTO");
		}

		var idMap = new Dictionary<string, Guid>();
		var cleanFootnotes = new List<FootnoteSegmentModel>();

		// Process footnotes if available
		if (dto.Footnotes is not null) {
			foreach (var note in dto.Footnotes) {
				if (note is null || string.IsNullOrEmpty(note.InitialId)) {
					continue;
				}

				var systemId = Guid.CreateVersion7();
				idMap[note.InitialId] = systemId;
				cleanFootnotes.Add(new FootnoteSegmentModel { Id = systemId, Segments = note.Segments });
			}
		}

		// Process content segments
		var cleanContent = new List<SegmentModel>();
		foreach (var segment in dto.Content!) {
			if (segment is TextSegmentModel textSeg) {
				var updatedRuns = textSeg.Runs.Select(run => {
					if (!string.IsNullOrEmpty(run.FootnoteId) &&
						idMap.TryGetValue(run.FootnoteId, out var systemId)) {
						return run with { FootnoteId = systemId.ToString() };
					}

					return run;
				}).ToList();

				cleanContent.Add(textSeg with { Runs = updatedRuns });
			}
			else {
				cleanContent.Add(segment);
			}
		}

		var chapterId = Guid.CreateVersion7();

		var chapter = new ChapterModel {
			Id = chapterId,
			VolumeId = volumeId,
			Order = dto.Order,
			Title = dto.Title,
			Status = ChapterStatus.Done
		};

		var content = new ChapterContentModel { Id = chapterId, Segments = cleanContent, Footnotes = cleanFootnotes };

		return Task.FromResult(new IngestionResult(chapter, content, null));
	}
}
