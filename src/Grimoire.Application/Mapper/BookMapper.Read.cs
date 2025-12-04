namespace Grimoire.Application.Mapper;

using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;
using Dto.Book.Segment;

public partial class BookMapper {
	public partial ChapterResponseDto ToChapterDto(ChapterModel model);
	public partial SeriesResponseDto ToSeriesDto(SeriesModel model);
	public partial VolumeResponseDto ToVolumeDto(VolumeModel model);

	private SegmentDto MapSegment(SegmentModel model) => model switch {
		TextSegmentModel t => ToTextDto(t),
		ImageSegmentModel i => ToImageDto(i),
		DividerSegmentModel d => ToDividerDto(d),
		FootnoteSegmentModel f => ToFootnoteDto(f),
		_ => throw new NotImplementedException($"Unknown segment type: {model.GetType().Name}")
	};
}
