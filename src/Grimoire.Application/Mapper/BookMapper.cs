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
	private partial TextSegmentDto ToTextDto(TextSegmentModel model);
	private partial ImageSegmentDto ToImageDto(ImageSegmentModel model);
	private partial DividerSegmentDto ToDividerDto(DividerSegmentModel model);
	private partial FootnoteSegmentDto ToFootnoteDto(FootnoteSegmentModel model);
	private partial TextRunDto ToTextRunDto(TextRun model);


	[MapperIgnoreTarget(nameof(BaseModel.CreatedAt))]
	[MapperIgnoreTarget(nameof(BaseModel.UpdatedAt))]
	[MapperIgnoreTarget(nameof(BaseModel.Id))]
	private partial TextSegmentModel ToTextSegment(TextSegmentDto dto);
}
