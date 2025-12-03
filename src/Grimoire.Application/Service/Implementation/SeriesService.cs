namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Exception;
using Dto.Book;

public sealed class SeriesService(ISeriesRepository repository) : ISeriesService {
	public async Task<SeriesModel?> FindOne(Guid id) => await repository.FindOne(id);

	public async Task<IEnumerable<SeriesModel>> FindAll() => await repository.FindAll();

	public async Task<SeriesModel> Create(CreateSeriesRequestDto dto) => await repository.Create(dto.ToModel());

	public async Task<SeriesModel> Update(Guid id, UpdateSeriesRequestDto dto) {
		var series = await repository.FindOne(id) ??
					throw new EntityNotFoundException($"Series with id {id} not found");
		series.Title = dto.Title ?? series.Title;
		series.Metadata = dto.Metadata ?? series.Metadata;

		return await repository.Update(series);
	}

	public async Task<int> Delete(Guid id) => await repository.Delete(id);
}
