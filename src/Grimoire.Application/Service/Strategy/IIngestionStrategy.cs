namespace Grimoire.Application.Service.Strategy;

using System.Threading;
using Dto.Book;

/// <summary>
///     Strategy interface for chapter ingestion
/// </summary>
public interface IIngestionStrategy {
	/// <summary>
	///     Determines if this strategy can handle the given DTO
	/// </summary>
	public bool CanHandle(CreateChapterRequestDto dto);

	/// <summary>
	///     Executes the ingestion strategy
	/// </summary>
	public Task<IngestionResult> ExecuteAsync(CreateChapterRequestDto dto, Guid volumeId, CancellationToken cancellationToken = default);
}
