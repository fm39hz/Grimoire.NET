namespace Grimoire.Domain.Common.Repository;

using Entity.Book;

public interface ISourceMaterialRepository : IRepository<SourceMaterial> {
	Task<IEnumerable<SourceMaterial>> FindBySeriesId(Guid seriesId);
}
