namespace Grimoire.Application.Mapper;

using Domain.Common;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;
using Dto.Book.Segment;
using Riok.Mapperly.Abstractions;

public partial class BookMapper {
#pragma warning disable RMG012
	[MapProperty(nameof(SeriesModel.Id), nameof(SeriesResponseDto.Id), Use = nameof(MapSeriesId))]
	public partial SeriesResponseDto ToSeriesDto(SeriesModel model);
#pragma warning restore RMG012

	[MapProperty(nameof(VolumeModel.Id), nameof(VolumeResponseDto.Id), Use = nameof(MapVolumeId))]
	[MapProperty(nameof(VolumeModel.SeriesId), nameof(VolumeResponseDto.SeriesId), Use = nameof(MapSeriesId))]
	public partial VolumeResponseDto ToVolumeDto(VolumeModel model);

	public ChapterResponseDto ToChapterDto(ChapterModel model) =>
		new() {
			Id = MapChapterId(model.Id),
			VolumeId = MapVolumeId(model.VolumeId),
			Title = model.Title,
			Order = model.Order,
			Content = model.ContentData?.Segments.Select(MapSegment).ToList() ?? [],
			Footnotes = model.ContentData?.Footnotes.Select(ToFootnoteDto).ToList() ?? [],
			CreatedAt = model.CreatedAt,
			UpdatedAt = model.UpdatedAt
		};

	[MapProperty(nameof(ChapterModel.Id), nameof(ChapterListResponseDto.Id), Use = nameof(MapChapterId))]
	[MapProperty(nameof(ChapterModel.VolumeId), nameof(ChapterListResponseDto.VolumeId), Use = nameof(MapVolumeId))]
	public partial ChapterListResponseDto ToChapterListDto(ChapterModel model);

	[MapProperty(nameof(AssetModel.Id), nameof(AssetResponseDto.Id), Use = nameof(MapAssetId))]
	[MapProperty(nameof(AssetModel.SeriesId), nameof(AssetResponseDto.SeriesId), Use = nameof(MapSeriesId))]
	public partial AssetResponseDto ToAssetDto(AssetModel model);

	private SegmentDto MapSegment(SegmentModel model) => model switch {
		TextSegmentModel t => ToTextDto(t),
		ImageSegmentModel i => ToImageDto(i),
		DividerSegmentModel d => ToDividerDto(d),
		FootnoteSegmentModel f => ToFootnoteDto(f),
		_ => throw new NotImplementedException($"Unknown segment type: {model.GetType().Name}")
	};

	// ID conversion helpers for Mapperly
	private static string MapSeriesId(Guid id) =>
		PrefixedId.ToString(EntityPrefix.Series, id);

	private static string MapVolumeId(Guid id) =>
		PrefixedId.ToString(EntityPrefix.Volume, id);

	private static string MapChapterId(Guid id) =>
		PrefixedId.ToString(EntityPrefix.Chapter, id);

	private static string MapSegmentId(Guid id) =>
		PrefixedId.ToString(EntityPrefix.Segment, id);

	private static string MapAssetId(Guid id) => PrefixedId.ToString(EntityPrefix.Asset, id);
}
