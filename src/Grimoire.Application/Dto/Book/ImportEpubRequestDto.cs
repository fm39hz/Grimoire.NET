namespace Grimoire.Application.Dto.Book;

public record ImportEpubRequestDto(
    CreateSeriesRequestDto Series,
    List<ImportVolumeDto>? Volumes
);
