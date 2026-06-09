namespace Grimoire.Application.Service.Strategy;

using Dto.Book;

/// <summary>
///     Factory for selecting the appropriate ingestion strategy
/// </summary>
public class IngestionStrategyFactory {
	private readonly IEnumerable<IIngestionStrategy> _strategies;

	public IngestionStrategyFactory(IEnumerable<IIngestionStrategy> strategies) {
		ArgumentNullException.ThrowIfNull(strategies);
		_strategies = strategies;
	}

	/// <summary>
	///     Gets the first strategy that can handle the given DTO
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when no strategy can handle the DTO</exception>
	public IIngestionStrategy GetStrategy(CreateChapterRequestDto dto) {
		ArgumentNullException.ThrowIfNull(dto);

		var strategy = _strategies.FirstOrDefault(s => s.CanHandle(dto)) ?? throw new InvalidOperationException(
			"No ingestion strategy found that can handle the provided DTO");

		return strategy;
	}
}
