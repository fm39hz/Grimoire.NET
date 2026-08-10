namespace Grimoire.Application.Service.Implementation;

using System.Threading;
using Contract;
using Domain.Common.Repository;
using Domain.Entity.Book.Segment;
using Domain.Exception;
using Dto.Book.Segment;

public sealed class SegmentService(ISegmentRepository segmentRepository, ISeriesRevisionService? revisionService = null) : ISegmentService {
	public async Task<TextSegmentModel> UpdateTextAsync(Guid segmentId, IReadOnlyList<TextRunDto> runs, CancellationToken cancellationToken = default) {
		var segment = await segmentRepository.FindOne(segmentId, cancellationToken) ??
			throw new EntityNotFoundException($"Segment with id {segmentId} not found");

		if (segment is not TextSegmentModel textSegment) {
			throw new InvalidOperationException(
				$"Segment {segmentId} is a {segment.GetType().Name}; only text segments can be edited by prose operations.");
		}

		textSegment.Runs = [.. runs.Select(static r => new TextRun(r.Text, r.IsBold, r.IsItalic, r.FootnoteId))];
		await segmentRepository.Update(textSegment, cancellationToken);
		if (revisionService is not null) await revisionService.AdvanceAsync(textSegment.Path.GetSeriesId(), cancellationToken);
		return textSegment;
	}
}
