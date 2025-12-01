namespace Grimoire.Application.Service.Contract;

using Domain.Entity.Book;

public interface ISeriesService : ICrudService<SeriesModel> {
	public Task<SeriesModel?> FindOne(string title);
}
