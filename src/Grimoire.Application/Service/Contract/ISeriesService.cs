namespace Grimoire.Application.Service.Contract;

using Domain.Common;
using Domain.Entity.Book;
using Dto.Book;
using Dto.Common;

public interface ISeriesService : ICrudService<SeriesModel, CreateSeriesRequestDto, UpdateSeriesRequestDto> {
	public Task<IEnumerable<VolumeModel>> FindAllVolumes(Guid seriesId);
	public Task<PagedResult<VolumeModel>> FindAllVolumes(Guid seriesId, PaginationRequest pagination);
}
