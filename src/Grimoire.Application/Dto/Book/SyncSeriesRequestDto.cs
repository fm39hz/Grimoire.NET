namespace Grimoire.Application.Dto.Book;

using Domain.Entity.Book;
using Metadata;

public record SyncSeriesRequestDto(List<SyncVolumeDto> Volumes);

public record SyncVolumeDto(
	double Order,
	string Title,
	VolumeMetadataDto? Metadata,
	List<SyncChapterDto> Chapters
);

public record SyncChapterDto(
	double Order,
	string Title,
	List<SegmentModel>? Content,
	List<ImportFootnoteDto>? Footnotes,
	string? RawContent
);
