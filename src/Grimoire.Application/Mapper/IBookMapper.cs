namespace Grimoire.Application.Mapper;

using Domain.Entity.Book;
using Dto.Book;

public interface IBookMapper {
	public ChapterResponseDto ToChapterDto(ChapterModel model);
	public ChapterListResponseDto ToChapterListDto(ChapterModel model);
	public SeriesResponseDto ToSeriesDto(SeriesModel model);
	public VolumeResponseDto ToVolumeDto(VolumeModel model);
	public AssetResponseDto ToAssetDto(AssetModel model);
	public SeriesModel CreateSeries(CreateSeriesRequestDto dto);
	public VolumeModel CreateVolume(CreateVolumeRequestDto dto);
	public ChapterModel CreateChapter(CreateChapterRequestDto dto);
	public void UpdateChapter(UpdateChapterRequestDto dto, ChapterModel model);
	public void UpdateVolume(UpdateVolumeRequestDto dto, VolumeModel model);
	public void UpdateSeries(UpdateSeriesRequestDto dto, SeriesModel model);
}
