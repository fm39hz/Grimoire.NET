namespace Grimoire.Domain.Common.Repository;

using Entity.Ingestion;

public interface IImportSourceRepository : IRepository<ImportSourceModel> {
	Task<ImportSourceModel?> FindByIdentity(string producerId, string externalKey, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ImportSourceModel>> FindByTargetSeries(Guid seriesId, CancellationToken cancellationToken = default);
}
