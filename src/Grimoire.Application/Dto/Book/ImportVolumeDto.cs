namespace Grimoire.Application.Dto.Book;

public record ImportVolumeDto(
    int Order,
    string? Title,
    List<ImportChapterDto>? Chapters
);
