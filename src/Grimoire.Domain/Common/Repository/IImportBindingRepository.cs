namespace Grimoire.Domain.Common.Repository;

using Entity.Ingestion;

public interface IImportBindingRepository : IRepository<ImportBindingModel> {
	Task<IReadOnlyList<ImportBindingModel>> FindBySource(Guid importSourceId, CancellationToken cancellationToken = default);
	Task<ImportBindingModel?> FindBySourceNode(Guid importSourceId, string externalNodeKey, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ImportBindingModel>> FindBySources(IReadOnlyCollection<Guid> importSourceIds, CancellationToken cancellationToken = default);
}
