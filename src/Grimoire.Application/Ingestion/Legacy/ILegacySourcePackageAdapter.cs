namespace Grimoire.Application.Ingestion.Legacy;

using Contract;
using Dto.Book;
using Import;

public interface ILegacySourcePackageAdapter {
	SourcePackageDto FromSeriesSync(Guid seriesId, SyncSeriesRequestDto request);
	SourcePackageDto FromEpub(Guid seriesId, NormalizedImport normalized, IReadOnlyList<NormalizedVolume> volumes, string jobId);
}
