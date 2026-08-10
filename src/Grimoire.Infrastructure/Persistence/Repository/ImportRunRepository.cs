namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Ingestion;
using Microsoft.EntityFrameworkCore;

public sealed class ImportRunRepository(ApplicationDbContext context)
	: CrudRepository<ImportRunModel>(context), IImportRunRepository {
	public Task<ImportRunModel?> FindByIdempotencyKey(string producerId, string idempotencyKey, CancellationToken cancellationToken = default) =>
		Entities.AsNoTracking().FirstOrDefaultAsync(run => run.ProducerId == producerId && run.IdempotencyKey == idempotencyKey, cancellationToken);

	public async Task<IReadOnlyList<ImportRunModel>> FindRecent(int limit, CancellationToken cancellationToken = default) =>
		await Entities.AsNoTracking().OrderByDescending(static run => run.StartedAt).Take(Math.Clamp(limit, 1, 200)).ToListAsync(cancellationToken);
}
