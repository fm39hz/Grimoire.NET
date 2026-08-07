namespace Grimoire.Application.Mapper;

using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;
using Dto.Book.Segment;

public interface IBookMapper {
	public ChapterResponseDto ToChapterDto(ChapterModel model);
	public ChapterResponseDto ToChapterDto(ChapterModel model, IEnumerable<SegmentModel> segments);
	public ChapterListResponseDto ToChapterListDto(ChapterModel model);
	public SeriesResponseDto ToSeriesDto(SeriesModel model);
	public VolumeResponseDto ToVolumeDto(VolumeModel model);
	public AssetResponseDto ToAssetDto(AssetModel model);
	public IngestionAuditResponseDto ToIngestionAuditDto(IngestionAuditRecord model);
	public IQueryable<VolumeResponseDto> ProjectToVolumeDto(IQueryable<VolumeModel> query);
	public IQueryable<ChapterListResponseDto> ProjectToChapterListDto(IQueryable<ChapterModel> query);
	public SeriesModel CreateSeries(CreateSeriesRequestDto dto);
	public TextSegmentDto ToTextSegmentDto(TextSegmentModel model);
	public SegmentDto ToSegmentDto(SegmentModel model);
	public SegmentModel MapToSegment(SegmentDto dto);
	public VolumeModel CreateVolume(CreateVolumeRequestDto dto);
	public ChapterModel CreateChapter(CreateChapterRequestDto dto);
	public void UpdateChapter(UpdateChapterRequestDto dto, ChapterModel model);
	public void UpdateVolume(UpdateVolumeRequestDto dto, VolumeModel model);
	public void UpdateSeries(UpdateSeriesRequestDto dto, SeriesModel model);
}
