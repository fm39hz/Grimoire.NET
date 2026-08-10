namespace Grimoire.Domain.Common.Repository;

using Entity.Ingestion;

public interface IImportRunRepository : IRepository<ImportRunModel> {
	Task<ImportRunModel?> FindByIdempotencyKey(string producerId, string idempotencyKey, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<ImportRunModel>> FindRecent(int limit, CancellationToken cancellationToken = default);
}
