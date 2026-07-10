namespace Grimoire.Infrastructure.Persistence.Repository;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Database;
using Grimoire.Domain.Common.Repository;
using Grimoire.Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;

public sealed class IngestionAuditRepository(ApplicationDbContext context)
	: CrudRepository<IngestionAuditRecord>(context), IIngestionAuditRepository {

	public async Task<IEnumerable<IngestionAuditRecord>> GetBySeriesIdAsync(Guid seriesId, int limit = 20, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.Where(r => r.SeriesId == seriesId)
			.OrderByDescending(r => r.StartedAt)
			.Take(limit)
			.ToListAsync(cancellationToken);
}
