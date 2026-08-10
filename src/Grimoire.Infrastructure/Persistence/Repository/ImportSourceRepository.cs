namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Ingestion;
using Microsoft.EntityFrameworkCore;

public sealed class ImportSourceRepository(ApplicationDbContext context)
	: CrudRepository<ImportSourceModel>(context), IImportSourceRepository {
	public Task<ImportSourceModel?> FindByIdentity(string producerId, string externalKey, CancellationToken cancellationToken = default) =>
		Entities.FirstOrDefaultAsync(source => source.ProducerId == producerId && source.ExternalKey == externalKey, cancellationToken);

	public async Task<IReadOnlyList<ImportSourceModel>> FindByTargetSeries(Guid seriesId, CancellationToken cancellationToken = default) =>
		await Entities.AsNoTracking().Where(source => source.TargetSeriesId == seriesId).ToListAsync(cancellationToken);
}
