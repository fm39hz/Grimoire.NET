namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Exception;
using Dto.Book;

public sealed class VolumeService(IVolumeRepository repository) : IVolumeService {
	public async Task<VolumeModel?> FindOne(Guid id) => await repository.FindOne(id);

	public async Task<IEnumerable<VolumeModel>> FindAll() => await repository.FindAll();

	public async Task<VolumeModel> Create(CreateVolumeRequestDto dto) => await repository.Create(dto.ToModel());

	public async Task<VolumeModel> Update(Guid id, UpdateVolumeRequestDto dto) {
		var volume = await repository.FindOne(id) ??
					throw new EntityNotFoundException($"Volume with id {id} not found");
		volume.Order = dto.Order ?? volume.Order;
		volume.Title = dto.Title ?? volume.Title;
		volume.Metadata = dto.Metadata ?? volume.Metadata;

		return await repository.Update(volume);
	}

	public async Task<int> Delete(Guid id) => await repository.Delete(id);
}
