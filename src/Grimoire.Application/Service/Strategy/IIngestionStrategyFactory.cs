namespace Grimoire.Application.Service.Strategy;

using Dto.Book;

public interface IIngestionStrategyFactory {
	IIngestionStrategy GetStrategy(CreateChapterRequestDto dto);
}
