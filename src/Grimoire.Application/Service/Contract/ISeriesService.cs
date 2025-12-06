namespace Grimoire.Application.Service.Contract;

using Common;
using Domain.Entity.Book;
using Dto.Book;
using Dto.Common;

public interface ISeriesService : ICrudService<SeriesModel, CreateSeriesRequestDto, UpdateSeriesRequestDto> {
	public Task<IEnumerable<VolumeModel>> FindAllVolumes(Guid seriesId);
	public Task<PagedResult<VolumeModel>> FindAllVolumes(Guid seriesId, PaginationRequest pagination);
}
