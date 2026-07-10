namespace Grimoire.Application.Service.Strategy;

using System.Threading;
using Common;
using Domain.Entity.Book;
using Dto.Book;

/// <summary>
///     Strategy for ingesting pre-processed content (Content is already segmented)
/// </summary>
public class PreProcessedIngestionStrategy : IIngestionStrategy {
	public bool CanHandle(CreateChapterRequestDto dto) =>
		// Can handle if Content array exists (even if empty)
		dto.Content is not null;

	public Task<IngestionResult> ExecuteAsync(CreateChapterRequestDto dto, Guid volumeId, CancellationToken cancellationToken = default) {
		if (!CanHandle(dto)) {
			throw new InvalidOperationException("This strategy cannot handle the provided DTO");
		}

		var remapResult = FootnoteRemapper.Remap(dto.Content!, dto.Footnotes);

		var chapterId = Guid.CreateVersion7();

		var chapter = new ChapterModel {
			Id = chapterId,
			Order = dto.Order,
			Title = dto.Title,
			Status = ChapterStatus.Done
		};

		var segments = new List<SegmentModel>();
		var order = 1.0;

		foreach (var seg in remapResult.Segments) {
			seg.Order = order++;
			segments.Add(seg);
		}

		foreach (var fn in remapResult.Footnotes) {
			fn.Order = order++;
			segments.Add(fn);
		}

		return Task.FromResult(new IngestionResult(chapter, segments, null));
	}
}
