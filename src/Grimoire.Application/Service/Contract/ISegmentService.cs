namespace Grimoire.Application.Service.Contract;

using Domain.Entity.Book.Segment;
using Dto.Book.Segment;

public interface ISegmentService {
	/// <summary>
	///     Updates the text runs of a single text segment. Rejects any non-text segment type —
	///     image/footnote/divider are layout and must never be mutated by a prose edit.
	/// </summary>
	public Task<TextSegmentModel> UpdateTextAsync(Guid segmentId, IReadOnlyList<TextRunDto> runs, CancellationToken cancellationToken = default);
}
