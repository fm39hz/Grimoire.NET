namespace Grimoire.Application.Mapper;

using Domain.Common;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;
using Dto.Book.Segment;
using Grimoire.Domain.Common.ValueObject;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

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

	public ChapterResponseDto ToChapterDto(ChapterModel model) => ToChapterDto(model, []);

	public ChapterResponseDto ToChapterDto(ChapterModel model, IEnumerable<SegmentModel> segments) {
		var contentSegments = segments.Where(static s => s is not FootnoteSegmentModel).OrderBy(static s => s.Order).ToList();
		var footnotes = segments.OfType<FootnoteSegmentModel>().ToList();
		return new ChapterResponseDto {
			Id = MapChapterId(model.Id),
			VolumeId = GetVolumeIdFromPath(model.Path),
			Title = model.Title,
			Order = model.Order,
			Content = [.. contentSegments.Select(MapSegment)],
			Footnotes = [.. footnotes.Select(ToFootnoteDto)],
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

	public IngestionAuditResponseDto ToIngestionAuditDto(IngestionAuditRecord model) => new(
		MapIngestionAuditId(model.Id),
		MapSeriesId(model.SeriesId),
		model.SourceType,
		model.Status,
		model.ErrorMessage,
		model.Summary,
		model.StartedAt,
		model.CompletedAt
	);

	private static string MapIngestionAuditId(Guid id) => PrefixedId.ToString(EntityPrefix.IngestionAudit, id);

	private static string GetSeriesIdFromPath(BookPath path) {
		var guid = path.GetSeriesId();
		return guid != Guid.Empty ? PrefixedId.ToString(EntityPrefix.Series, guid) : string.Empty;
	}

	private static string GetVolumeIdFromPath(BookPath path) {
		var guid = path.GetVolumeId();
		return guid != Guid.Empty ? PrefixedId.ToString(EntityPrefix.Volume, guid) : string.Empty;
	}

	public IQueryable<VolumeResponseDto> ProjectToVolumeDto(IQueryable<VolumeModel> query) =>
		query.Select(static v => new VolumeResponseDto {
			Id = "vol_" + v.Id,
			SeriesId = "ser_" + ((string)v.DbPath).Substring(1, 8) + "-" + ((string)v.DbPath).Substring(9, 4) + "-" + ((string)v.DbPath).Substring(13, 4) + "-" + ((string)v.DbPath).Substring(17, 4) + "-" + ((string)v.DbPath).Substring(21, 12),
			Title = v.Title,
			Order = v.Order,
			CreatedAt = v.CreatedAt,
			UpdatedAt = v.UpdatedAt
		});

	public IQueryable<ChapterListResponseDto> ProjectToChapterListDto(IQueryable<ChapterModel> query) =>
		query.Select(static c => new ChapterListResponseDto {
			Id = "chap_" + c.Id,
			VolumeId = "vol_" + ((string)c.DbPath).Substring(35, 8) + "-" + ((string)c.DbPath).Substring(43, 4) + "-" + ((string)c.DbPath).Substring(47, 4) + "-" + ((string)c.DbPath).Substring(51, 4) + "-" + ((string)c.DbPath).Substring(55, 12),
			Title = c.Title,
			Order = c.Order,
			UpdatedAt = c.UpdatedAt
		});
}
