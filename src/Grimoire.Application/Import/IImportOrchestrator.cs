namespace Grimoire.Application.Import;

using Dto.Book;

public interface IImportOrchestrator {
    Task<ImportEpubResultDto> ImportAsync(
        IImportStrategy strategy,
        CreateSeriesRequestDto seriesDto,
        List<ImportVolumeDto>? volumesOverride,
        Stream sourceStream,
        CancellationToken cancellationToken = default);
}
