namespace Grimoire.Domain.Common.Repository;

using Entity.Ingestion;

public interface ISeriesResearchProfileRepository : IRepository<SeriesResearchProfileModel> {
	Task<SeriesResearchProfileModel?> FindBySeries(Guid seriesId, CancellationToken cancellationToken = default);
}
