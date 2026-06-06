namespace Grimoire.Application.Dto.Book;

public record ImportEpubResultDto(
    Guid SeriesId,
    int VolumesCreated,
    int VolumesUpdated,
    int ChaptersCreated,
    int ChaptersUpdated
);
