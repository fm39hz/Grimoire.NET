namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common.Repository;
using Domain.Entity.Book;

public sealed class SeriesService(ISeriesRepository repository) : ISeriesService {
	public async Task<SeriesModel?> FindOne(Guid id) => await repository.FindOne(id);

	public async Task<IEnumerable<SeriesModel>> FindAll() => await repository.FindAll();

	public async Task<SeriesModel> Create(SeriesModel entity) => await repository.Create(entity);

	public async Task<SeriesModel> Update(Guid id, SeriesModel entity) {
		var series = new SeriesModel(entity) { Id = id, Title = entity.Title };
		return await repository.Update(series);
	}

	public async Task<int> Delete(Guid id) => await repository.Delete(id);

	public async Task<SeriesModel?> FindOne(string title) => await repository.GetAsync(title);
}
