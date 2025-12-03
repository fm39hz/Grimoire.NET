namespace Grimoire.Application.Dto.Book;

using Common;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using System;
using System.Collections.Generic;

public class ChapterVariantResponseDto : IResponseDto {
    public Guid Id { get; init; }
    public Guid ChapterId { get; init; }
    public VariantType Type { get; init; }
    public string Language { get; init; }
    public string? SourceName { get; init; }
    public int WordCount { get; init; }
    public List<SegmentModel> Content { get; init; }
    public List<FootnoteSegmentModel> Footnotes { get; init; }

    public ChapterVariantResponseDto(ChapterVariantModel variant) {
        Id = variant.Id;
        ChapterId = variant.ChapterId;
        Type = variant.Type;
        Language = variant.Language;
        SourceName = variant.SourceName;
        WordCount = variant.WordCount;
        Content = variant.Content;
        Footnotes = variant.Footnotes;
    }
}
