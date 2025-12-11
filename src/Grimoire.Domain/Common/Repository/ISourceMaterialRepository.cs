namespace Grimoire.Domain.Common.Repository;

using Entity.Book;

public interface ISourceMaterialRepository : IRepository<SourceMaterial> {
	public Task<IEnumerable<SourceMaterial>> FindBySeriesId(Guid seriesId);
}
