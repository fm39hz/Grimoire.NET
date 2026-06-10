namespace Grimoire.Application.Service.Implementation;

using System.Threading;
using Contract;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Dto.Book;
using Dto.Common;

public sealed class VolumeService(
	IVolumeRepository repository,
	IVolumeNodeService volumeNodeService,
	IChapterNodeService chapterNodeService,
	INodeManagerService nodeManagerService) : CrudServiceBase<VolumeModel>, IVolumeService {
	public async Task<VolumeModel?> FindOne(Guid id, CancellationToken cancellationToken = default) => await repository.FindOne(id, cancellationToken);

	public async Task<PagedResult<VolumeModel>> FindAll(PaginationRequest request, CancellationToken cancellationToken = default) =>
		await GetPagedResultAsync(repository, request, cancellationToken);

	public async Task<VolumeModel> Create(CreateVolumeRequestDto dto, CancellationToken cancellationToken = default) {
		return await volumeNodeService.CreateVolume(dto, cancellationToken);
	}

	public async Task<VolumeModel> Update(Guid id, UpdateVolumeRequestDto dto, CancellationToken cancellationToken = default) {
		return await volumeNodeService.UpdateVolume(id, dto, cancellationToken);
	}

	public async Task<int> Delete(Guid id, CancellationToken cancellationToken = default) => await nodeManagerService.DeleteSubtree(id, cancellationToken);

	public async Task<IEnumerable<ChapterModel>> FindAllChapters(Guid volumeId, CancellationToken cancellationToken = default) =>
		await chapterNodeService.FindChapters(volumeId, cancellationToken);

	public async Task<PagedResult<ChapterModel>> FindAllChapters(Guid volumeId,
		PaginationRequest pagination, CancellationToken cancellationToken = default) =>
		await chapterNodeService.FindChapters(volumeId, pagination, cancellationToken);
}
