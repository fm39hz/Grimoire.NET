namespace Grimoire.Application.Service.Strategy;

using Dto.Book;

/// <summary>
///     Strategy interface for chapter ingestion
/// </summary>
public interface IIngestionStrategy {
	/// <summary>
	///     Determines if this strategy can handle the given DTO
	/// </summary>
	bool CanHandle(CreateChapterRequestDto dto);

	/// <summary>
	///     Executes the ingestion strategy
	/// </summary>
	Task<IngestionResult> ExecuteAsync(CreateChapterRequestDto dto);
}
