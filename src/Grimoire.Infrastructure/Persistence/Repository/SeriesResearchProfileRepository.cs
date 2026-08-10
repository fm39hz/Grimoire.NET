namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Ingestion;
using Microsoft.EntityFrameworkCore;

public sealed class SeriesResearchProfileRepository(ApplicationDbContext context)
	: CrudRepository<SeriesResearchProfileModel>(context), ISeriesResearchProfileRepository {
	public Task<SeriesResearchProfileModel?> FindBySeries(Guid seriesId, CancellationToken cancellationToken = default) =>
		Entities.FirstOrDefaultAsync(profile => profile.SeriesId == seriesId, cancellationToken);
}
