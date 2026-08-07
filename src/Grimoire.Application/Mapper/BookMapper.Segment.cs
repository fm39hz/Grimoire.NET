namespace Grimoire.Application.Mapper;

using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book.Segment;

public partial class BookMapper {
	/// <summary>
	///     Converts a wire-level <see cref="SegmentDto"/> into its domain <see cref="SegmentModel"/>.
	///     The DTOs mirror the models property-for-property; this is a lossless forward map used on
	///     the write path (create/update/sync). The reverse lives in the response mapper.
	/// </summary>
	public SegmentModel MapToSegment(SegmentDto dto) => dto switch {
		TextSegmentDto t => MapToTextSegment(t),
		ImageSegmentDto i => MapToImageSegment(i),
		DividerSegmentDto d => MapToDividerSegment(d),
		FootnoteSegmentDto f => MapToFootnoteSegment(f),
		_ => throw new NotSupportedException($"Segment type '{dto.GetType().Name}' mapping is not supported.")
	};

	public TextSegmentModel MapToTextSegment(TextSegmentDto dto) => new() {
		Id = ParseId(dto.Id),
		Runs = [.. dto.Runs.Select(static r => new TextRun(
			r.Text,
			r.IsBold,
			r.IsItalic,
			r.FootnoteId))]
	};

	public ImageSegmentModel MapToImageSegment(ImageSegmentDto dto) => new() {
		Id = ParseId(dto.Id),
		AssetKey = dto.AssetKey,
		Caption = dto.Caption
	};

	public DividerSegmentModel MapToDividerSegment(DividerSegmentDto dto) => new() {
		Id = ParseId(dto.Id),
		Style = dto.Style
	};

	public FootnoteSegmentModel MapToFootnoteSegment(FootnoteSegmentDto dto) => new() {
		Id = ParseId(dto.Id),
		Segments = [.. dto.Segments.Select(MapToTextSegment)]
	};

	public TextRunDto MapToTextRunDto(TextRun run) => new(
		run.Text,
		run.IsBold,
		run.IsItalic,
		run.FootnoteId);

	/// <summary>
	///     Converts a domain <see cref="SegmentModel"/> back to its wire <see cref="SegmentDto"/>.
	///     Used by the import path to hand already-parsed segments back to the request DTO shape.
	/// </summary>
	public SegmentDto ToSegmentDto(SegmentModel model) => model switch {
		TextSegmentModel t => new TextSegmentDto {
			Id = t.Id.ToString(),
			Runs = [.. t.Runs.Select(MapToTextRunDto)]
		},
		ImageSegmentModel i => new ImageSegmentDto {
			Id = i.Id.ToString(),
			AssetKey = i.AssetKey,
			Caption = i.Caption
		},
		DividerSegmentModel d => new DividerSegmentDto {
			Id = d.Id.ToString(),
			Style = d.Style
		},
		FootnoteSegmentModel f => new FootnoteSegmentDto {
			Id = f.Id.ToString(),
			Segments = [.. f.Segments.Select(ToTextSegmentDto)]
		},
		_ => throw new NotSupportedException($"Segment type '{model.GetType().Name}' mapping is not supported.")
	};

	private static Guid ParseId(string? id) =>
		Guid.TryParse(id, out var guid) ? guid : Guid.CreateVersion7();
}
