namespace Grimoire.Application.Mapper;

using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;
using Dto.Book.Segment;

public interface IBookMapper {
	public ChapterResponseDto ToChapterDto(ChapterModel model);
	public ChapterListResponseDto ToChapterListDto(ChapterModel model);
	public SeriesResponseDto ToSeriesDto(SeriesModel model);
	public VolumeResponseDto ToVolumeDto(VolumeModel model);
	public AssetResponseDto ToAssetDto(AssetModel model);
	public System.Linq.IQueryable<VolumeResponseDto> ProjectToVolumeDto(System.Linq.IQueryable<VolumeModel> query);
	public System.Linq.IQueryable<ChapterListResponseDto> ProjectToChapterListDto(System.Linq.IQueryable<ChapterModel> query);
	public SeriesModel CreateSeries(CreateSeriesRequestDto dto);
	public TextSegmentDto ToTextSegmentDto(TextSegmentModel model);
	public VolumeModel CreateVolume(CreateVolumeRequestDto dto, Guid seriesId);
	public ChapterModel CreateChapter(CreateChapterRequestDto dto, Guid volumeId);
	public void UpdateChapter(UpdateChapterRequestDto dto, ChapterModel model);
	public void UpdateVolume(UpdateVolumeRequestDto dto, VolumeModel model);
	public void UpdateSeries(UpdateSeriesRequestDto dto, SeriesModel model);
	public void MergeChapter(ChapterModel source, ChapterContentModel sourceContent, ChapterModel target);
}
