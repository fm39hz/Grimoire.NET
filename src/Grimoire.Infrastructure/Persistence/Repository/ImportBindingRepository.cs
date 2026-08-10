namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Ingestion;
using Microsoft.EntityFrameworkCore;

public sealed class ImportBindingRepository(ApplicationDbContext context)
	: CrudRepository<ImportBindingModel>(context), IImportBindingRepository {
	public async Task<IReadOnlyList<ImportBindingModel>> FindBySource(Guid importSourceId, CancellationToken cancellationToken = default) =>
		await Entities.AsNoTracking().Where(binding => binding.ImportSourceId == importSourceId).ToListAsync(cancellationToken);

	public Task<ImportBindingModel?> FindBySourceNode(Guid importSourceId, string externalNodeKey, CancellationToken cancellationToken = default) =>
		Entities.FirstOrDefaultAsync(binding => binding.ImportSourceId == importSourceId && binding.ExternalNodeKey == externalNodeKey, cancellationToken);

	public async Task<IReadOnlyList<ImportBindingModel>> FindBySources(IReadOnlyCollection<Guid> importSourceIds, CancellationToken cancellationToken = default) =>
		await Entities.AsNoTracking().Where(binding => importSourceIds.Contains(binding.ImportSourceId)).ToListAsync(cancellationToken);
}
