namespace Grimoire.Domain.Common.Repository;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Entity.Book;

public interface IIngestionAuditRepository : IRepository<IngestionAuditRecord> {
	public Task<IEnumerable<IngestionAuditRecord>> GetBySeriesIdAsync(Guid seriesId, int limit = 20, CancellationToken cancellationToken = default);
}
