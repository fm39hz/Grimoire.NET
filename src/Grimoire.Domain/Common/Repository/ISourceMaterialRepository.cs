namespace Grimoire.Domain.Common.Repository;

using System.Threading;
using Entity.Book;

public interface ISourceMaterialRepository : IRepository<SourceMaterial> {
	public Task<IEnumerable<SourceMaterial>> FindBySeriesId(Guid seriesId, CancellationToken cancellationToken = default);
}
