namespace Grimoire.Application.Mapper;

using Domain.Common;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;
using Dto.Book.Segment;
using Riok.Mapperly.Abstractions;
using Microsoft.EntityFrameworkCore;

public partial class BookMapper {
#pragma warning disable RMG012
	[MapProperty(nameof(SeriesModel.Id), nameof(SeriesResponseDto.Id), Use = nameof(MapSeriesId))]
	public partial SeriesResponseDto ToSeriesDto(SeriesModel model);
#pragma warning restore RMG012

	public VolumeResponseDto ToVolumeDto(VolumeModel model) => new() {
		Id = MapVolumeId(model.Id),
		SeriesId = GetSeriesIdFromPath(model.Path),
		Title = model.Title,
		Order = model.Order,
		CreatedAt = model.CreatedAt,
		UpdatedAt = model.UpdatedAt
	};

	public ChapterResponseDto ToChapterDto(ChapterModel model) => ToChapterDto(model, Array.Empty<SegmentModel>());

	public ChapterResponseDto ToChapterDto(ChapterModel model, IEnumerable<SegmentModel> segments) {
		var contentSegments = segments.Where(s => s is not FootnoteSegmentModel).OrderBy(s => s.Order).ToList();
		var footnotes = segments.OfType<FootnoteSegmentModel>().ToList();
		return new ChapterResponseDto {
			Id = MapChapterId(model.Id),
			VolumeId = GetVolumeIdFromPath(model.Path),
			Title = model.Title,
			Order = model.Order,
			Content = contentSegments.Select(MapSegment).ToList(),
			Footnotes = footnotes.Select(ToFootnoteDto).ToList(),
			CreatedAt = model.CreatedAt,
			UpdatedAt = model.UpdatedAt
		};
	}

	public ChapterListResponseDto ToChapterListDto(ChapterModel model) => new() {
		Id = MapChapterId(model.Id),
		VolumeId = GetVolumeIdFromPath(model.Path),
		Title = model.Title,
		Order = model.Order,
		UpdatedAt = model.UpdatedAt
	};

	[MapProperty(nameof(AssetModel.Id), nameof(AssetResponseDto.Id), Use = nameof(MapAssetId))]
	[MapProperty(nameof(AssetModel.SeriesId), nameof(AssetResponseDto.SeriesId), Use = nameof(MapSeriesId))]
	public partial AssetResponseDto ToAssetDto(AssetModel model);

	private SegmentDto MapSegment(SegmentModel model) => model switch {
		TextSegmentModel t => ToTextSegmentDto(t),
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

	private static string GetSeriesIdFromPath(LTree path) {
		string pathStr = path.ToString();
		if (string.IsNullOrEmpty(pathStr)) return string.Empty;
		var parts = pathStr.Split('.');
		if (parts.Length > 0 && parts[0].Length > 1 && parts[0].StartsWith('n')) {
			if (Guid.TryParseExact(parts[0][1..], "N", out var guid)) {
				return PrefixedId.ToString(EntityPrefix.Series, guid);
			}
		}
		return string.Empty;
	}

	private static string GetVolumeIdFromPath(LTree path) {
		string pathStr = path.ToString();
		if (string.IsNullOrEmpty(pathStr)) return string.Empty;
		var parts = pathStr.Split('.');
		if (parts.Length > 1 && parts[1].Length > 1 && parts[1].StartsWith('n')) {
			if (Guid.TryParseExact(parts[1][1..], "N", out var guid)) {
				return PrefixedId.ToString(EntityPrefix.Volume, guid);
			}
		}
		return string.Empty;
	}

	public System.Linq.IQueryable<VolumeResponseDto> ProjectToVolumeDto(System.Linq.IQueryable<VolumeModel> query) =>
		query.Select(v => new VolumeResponseDto {
			Id = "vol_" + v.Id,
			SeriesId = "ser_" + ((string)v.Path).Substring(1, 8) + "-" + ((string)v.Path).Substring(9, 4) + "-" + ((string)v.Path).Substring(13, 4) + "-" + ((string)v.Path).Substring(17, 4) + "-" + ((string)v.Path).Substring(21, 12),
			Title = v.Title,
			Order = v.Order,
			CreatedAt = v.CreatedAt,
			UpdatedAt = v.UpdatedAt
		});

	public System.Linq.IQueryable<ChapterListResponseDto> ProjectToChapterListDto(System.Linq.IQueryable<ChapterModel> query) =>
		query.Select(c => new ChapterListResponseDto {
			Id = "chap_" + c.Id,
			VolumeId = "vol_" + ((string)c.Path).Substring(35, 8) + "-" + ((string)c.Path).Substring(43, 4) + "-" + ((string)c.Path).Substring(47, 4) + "-" + ((string)c.Path).Substring(51, 4) + "-" + ((string)c.Path).Substring(55, 12),
			Title = c.Title,
			Order = c.Order,
			UpdatedAt = c.UpdatedAt
		});
}
