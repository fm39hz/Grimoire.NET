namespace Grimoire.Application.Mapper;

using System.Diagnostics.CodeAnalysis;
using Domain.Entity;
using Domain.Entity.Book.Segment;
using Dto.Book.Segment;
using Riok.Mapperly.Abstractions;

[Mapper]
[SuppressMessage("Mapper", "RMG020:Source member is not mapped to any target member")]
[SuppressMessage("Mapper", "RMG089:Mapping nullable source to non-nullable target member")]
public partial class BookMapper : IBookMapper {
	[MapProperty(nameof(TextSegmentModel.Id), nameof(TextSegmentDto.Id), Use = nameof(MapSegmentId))]
	private partial TextSegmentDto ToTextDto(TextSegmentModel model);

	[MapProperty(nameof(ImageSegmentModel.Id), nameof(ImageSegmentDto.Id), Use = nameof(MapSegmentId))]
	private partial ImageSegmentDto ToImageDto(ImageSegmentModel model);

	[MapProperty(nameof(DividerSegmentModel.Id), nameof(DividerSegmentDto.Id), Use = nameof(MapSegmentId))]
	private partial DividerSegmentDto ToDividerDto(DividerSegmentModel model);

	[MapProperty(nameof(FootnoteSegmentModel.Id), nameof(FootnoteSegmentDto.Id), Use = nameof(MapSegmentId))]
	private partial FootnoteSegmentDto ToFootnoteDto(FootnoteSegmentModel model);

	private partial TextRunDto ToTextRunDto(TextRun model);


	[MapperIgnoreTarget(nameof(BaseModel.Id))]
	private partial TextSegmentModel ToTextSegment(TextSegmentDto dto);
}
