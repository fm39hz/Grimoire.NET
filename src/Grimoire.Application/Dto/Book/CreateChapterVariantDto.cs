namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book;
using Domain.Entity.Book.Segment;

public record CreateChapterVariantDto(
    VariantType Type,
    string Language,
    string? SourceName,
    int? WordCount,
    List<SegmentModel> Content,
    List<ImportFootnoteDto> Footnotes
);
