namespace Grimoire.Application.Mapper;

using Common;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;
using Dto.Book.Segment;
using Riok.Mapperly.Abstractions;

public partial class BookMapper {
	[MapProperty(nameof(SeriesModel.Id), nameof(SeriesResponseDto.Id), Use = nameof(MapSeriesId))]
	public partial SeriesResponseDto ToSeriesDto(SeriesModel model);
	
	[MapProperty(nameof(VolumeModel.Id), nameof(VolumeResponseDto.Id), Use = nameof(MapVolumeId))]
	[MapProperty(nameof(VolumeModel.SeriesId), nameof(VolumeResponseDto.SeriesId), Use = nameof(MapSeriesId))]
	public partial VolumeResponseDto ToVolumeDto(VolumeModel model);
	
	[MapProperty(nameof(ChapterModel.Id), nameof(ChapterResponseDto.Id), Use = nameof(MapChapterId))]
	[MapProperty(nameof(ChapterModel.VolumeId), nameof(ChapterResponseDto.VolumeId), Use = nameof(MapVolumeId))]
	public partial ChapterResponseDto ToChapterDto(ChapterModel model);
	
	[MapProperty(nameof(ChapterModel.Id), nameof(ChapterListResponseDto.Id), Use = nameof(MapChapterId))]
	[MapProperty(nameof(ChapterModel.VolumeId), nameof(ChapterListResponseDto.VolumeId), Use = nameof(MapVolumeId))]
	public partial ChapterListResponseDto ToChapterListDto(ChapterModel model);

	private SegmentDto MapSegment(SegmentModel model) => model switch {
		TextSegmentModel t => ToTextDto(t),
		ImageSegmentModel i => ToImageDto(i),
		DividerSegmentModel d => ToDividerDto(d),
		FootnoteSegmentModel f => ToFootnoteDto(f),
		_ => throw new NotImplementedException($"Unknown segment type: {model.GetType().Name}")
	};
	
	// ID conversion helpers for Mapperly
	private string MapSeriesId(Guid id) => PrefixedId.ToString(EntityPrefix.Series, id);
	private string MapVolumeId(Guid id) => PrefixedId.ToString(EntityPrefix.Volume, id);
	private string MapChapterId(Guid id) => PrefixedId.ToString(EntityPrefix.Chapter, id);
	private string MapSegmentId(Guid id) => PrefixedId.ToString(EntityPrefix.Segment, id);
}
