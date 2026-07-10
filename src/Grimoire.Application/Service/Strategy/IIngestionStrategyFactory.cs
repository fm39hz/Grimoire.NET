namespace Grimoire.Application.Service.Strategy;

using Dto.Book;

public interface IIngestionStrategyFactory {
	public IIngestionStrategy GetStrategy(CreateChapterRequestDto dto);
}
