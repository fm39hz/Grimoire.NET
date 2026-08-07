namespace Grimoire.Application.Dto.Book;

using Metadata;
using Segment;

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
	List<SegmentDto>? Content,
	List<ImportFootnoteDto>? Footnotes,
	string? RawContent
);
