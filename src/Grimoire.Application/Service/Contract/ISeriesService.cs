namespace Grimoire.Application.Service.Contract;

using System.Threading;
using Domain.Common;
using Domain.Entity.Book;
using Dto.Book;
using Dto.Common;

public interface ISeriesService : ICrudService<SeriesModel, CreateSeriesRequestDto, UpdateSeriesRequestDto> {
	public Task<(SeriesModel Series, bool Created)> GetOrCreate(CreateSeriesRequestDto dto, CancellationToken cancellationToken = default);
	public Task<IEnumerable<VolumeModel>> FindAllVolumes(Guid seriesId, CancellationToken cancellationToken = default);
	public Task<PagedResult<VolumeModel>> FindAllVolumes(Guid seriesId, PaginationRequest pagination, CancellationToken cancellationToken = default);
}
