namespace Grimoire.Application.Ingestion.Research;

using Contract;

public interface IResearchCoordinator {
	Task<SeriesResearchProfileDto> Research(Guid seriesId, bool forceRefresh = false, CancellationToken cancellationToken = default);
	Task<SeriesResearchProfileDto?> Find(Guid seriesId, CancellationToken cancellationToken = default);
	Task<SeriesResearchProfileDto> Confirm(Guid seriesId, ConfirmResearchEvidenceDto request, CancellationToken cancellationToken = default);
	Task ObserveSourcePackage(Guid seriesId, SourcePackageDto package, CancellationToken cancellationToken = default);
}
