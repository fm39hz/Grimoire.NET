namespace Grimoire.Application.Service.Contract;

using Domain.Common;
using Domain.Entity.Book;
using Dto.Book;
using Dto.Book.Tree;
using Dto.Common;

public interface ISeriesNodeService {
	public Task<BookTreeDto> GetTree(Guid seriesId, bool includeContent = false, CancellationToken cancellationToken = default);
	public Task<SeriesModel?> FindSeries(Guid seriesId, CancellationToken cancellationToken = default);
	public Task<SeriesModel> CreateSeries(CreateSeriesRequestDto dto, CancellationToken cancellationToken = default);
	public Task<(SeriesModel Series, bool Created)> GetOrCreateSeries(CreateSeriesRequestDto dto, CancellationToken cancellationToken = default);
	public Task<SeriesModel> UpdateSeries(Guid seriesId, UpdateSeriesRequestDto dto, CancellationToken cancellationToken = default);
}

public interface IVolumeNodeService {
	public Task<VolumeModel> CreateVolume(CreateVolumeRequestDto dto, CancellationToken cancellationToken = default);
	public Task<VolumeModel> UpdateVolume(Guid volumeId, UpdateVolumeRequestDto dto, CancellationToken cancellationToken = default);
	public Task<IEnumerable<VolumeModel>> FindVolumes(Guid seriesId, CancellationToken cancellationToken = default);
	public Task<PagedResult<VolumeModel>> FindVolumes(Guid seriesId, PaginationRequest pagination, CancellationToken cancellationToken = default);
}

public interface IChapterNodeService {
	public Task<IEnumerable<ChapterModel>> FindChapters(Guid volumeId, CancellationToken cancellationToken = default);
	public Task<PagedResult<ChapterModel>> FindChapters(Guid volumeId, PaginationRequest pagination, CancellationToken cancellationToken = default);
}

public interface INodeManagerService {
	public Task<BookNodeModel> CreateNode(Guid id, BookNodeType type, Guid? parentId, string title, float order, CancellationToken cancellationToken = default);
	public Task<BookNodeModel> UpdateNode(Guid id, string? title, float? order, CancellationToken cancellationToken = default);
	public Task MoveNode(Guid nodeId, Guid? newParentId, float newOrder, CancellationToken cancellationToken = default);
	public Task<int> DeleteSubtree(Guid nodeId, CancellationToken cancellationToken = default);
}

public interface IBookTreeService : ISeriesNodeService, IVolumeNodeService, IChapterNodeService, INodeManagerService {
}
