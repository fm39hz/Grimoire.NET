namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common.Repository;
using Domain.Entity.Book;

public sealed class VolumeService(IVolumeRepository repository) : IVolumeService {
	public async Task<VolumeModel?> FindOne(Guid id) => await repository.FindOne(id);

	public async Task<IEnumerable<VolumeModel>> FindAll() => await repository.FindAll();

	public async Task<VolumeModel> Create(VolumeModel entity) => await repository.Create(entity);

	public async Task<VolumeModel> Update(Guid id, VolumeModel entity) {
		var volume = new VolumeModel {
			Id = id,
			SeriesId = entity.SeriesId,
			Order = entity.Order,
			Title = entity.Title,
			Metadata = entity.Metadata
		};
		return await repository.Update(volume);
	}

	public async Task<int> Delete(Guid id) => await repository.Delete(id);
}
