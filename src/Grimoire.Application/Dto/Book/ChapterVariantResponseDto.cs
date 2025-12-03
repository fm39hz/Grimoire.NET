namespace Grimoire.Application.Dto.Book;

using Common;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using System;
using System.Collections.Generic;

public class ChapterVariantResponseDto(ChapterVariantModel variant) : IResponseDto {
	public Guid Id { get; init; } = variant.Id;
	public Guid ChapterId { get; init; } = variant.ChapterId;
	public VariantType Type { get; init; } = variant.Type;
	public string Language { get; init; } = variant.Language;
	public string? SourceName { get; init; } = variant.SourceName;
	public int WordCount { get; init; } = variant.WordCount;
	public List<SegmentModel> Content { get; init; } = variant.Content;
	public List<FootnoteSegmentModel> Footnotes { get; init; } = variant.Footnotes;
}
