namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Exception;
using Dto.Book;
using Dto.Common;
using Extensions;
using Mapper;

public sealed class SeriesService(ISeriesRepository repository, IBookMapper mapper) : ISeriesService {
	public async Task<SeriesModel?> FindOne(Guid id) => await repository.FindOne(id);

	public async Task<IEnumerable<SeriesModel>> FindAll() => await repository.FindAll();

	public async Task<PagedResult<SeriesModel>> FindAllPaged(PaginationRequest request) {
		var allItems = await repository.FindAll();
		var totalCount = allItems.Count();
		
		var items = allItems
			.Skip((request.PageIndex - 1) * request.PageSize)
			.Take(request.PageSize)
			.ToList();
		
		return new PagedResult<SeriesModel>(items, totalCount, request.PageIndex, request.PageSize);
	}

	public async Task<SeriesModel> Create(CreateSeriesRequestDto dto) {
		var series = mapper.CreateSeries(dto);
		return await repository.Create(series);
	}

	public async Task<SeriesModel> Update(Guid id, UpdateSeriesRequestDto dto) {
		var series = await repository.FindOne(id) ??
					throw new EntityNotFoundException($"Series with id {id} not found");
		mapper.UpdateSeries(dto, series);
		return await repository.Update(series);
	}

	public async Task<int> Delete(Guid id) => await repository.Delete(id);
}
