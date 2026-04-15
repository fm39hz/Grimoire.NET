namespace Grimoire.Application.Mapper;

using Common;
using Domain.Entity;
using Domain.Entity.Book;
using Domain.Entity.Book.Metadata;
using Dto.Book;
using Dto.Book.Metadata;
using Riok.Mapperly.Abstractions;

public partial class BookMapper {
#pragma warning disable RMG012
	[MapperIgnoreTarget(nameof(BaseModel.CreatedAt))]
	[MapperIgnoreTarget(nameof(BaseModel.UpdatedAt))]
	[MapperIgnoreTarget(nameof(BaseModel.Id))]
	[MapperIgnoreTarget(nameof(SeriesModel.GlossaryTerms))]
	[MapperIgnoreTarget(nameof(SeriesModel.SourceMaterials))]
	public partial SeriesModel CreateSeries(CreateSeriesRequestDto dto);
#pragma warning restore RMG012

	public VolumeModel CreateVolume(CreateVolumeRequestDto dto, Guid seriesId) => new() {
		SeriesId = seriesId,
		Order = dto.Order,
		Title = dto.Title,
		Metadata = dto.Metadata != null ? ToVolumeMetadata(dto.Metadata) : null
	};

	public ChapterModel CreateChapter(CreateChapterRequestDto dto, Guid volumeId) {
		var remapResult = FootnoteRemapper.Remap(dto.Content ?? [], dto.Footnotes);

		var chapterId = Guid.CreateVersion7();
		return new ChapterModel {
			Id = chapterId,
			VolumeId = volumeId,
			Order = dto.Order,
			Title = dto.Title,
			ContentData = new ChapterContentModel {
				Id = chapterId,
				Segments = remapResult.Segments,
				Footnotes = remapResult.Footnotes
			}
		};
	}

#pragma warning disable RMG012
	[MapperIgnoreTarget(nameof(BaseModel.CreatedAt))]
	[MapperIgnoreTarget(nameof(BaseModel.UpdatedAt))]
	[MapperIgnoreTarget(nameof(BaseModel.Id))]
	public partial void UpdateChapter(UpdateChapterRequestDto dto, [MappingTarget] ChapterModel model);
#pragma warning restore RMG012
	[MapperIgnoreTarget(nameof(BaseModel.CreatedAt))]
	[MapperIgnoreTarget(nameof(BaseModel.UpdatedAt))]
	[MapperIgnoreTarget(nameof(BaseModel.Id))]
	public partial void UpdateVolume(UpdateVolumeRequestDto dto, [MappingTarget] VolumeModel model);

#pragma warning disable RMG012
	[MapperIgnoreTarget(nameof(BaseModel.CreatedAt))]
	[MapperIgnoreTarget(nameof(BaseModel.UpdatedAt))]
	[MapperIgnoreTarget(nameof(BaseModel.Id))]
	public partial void UpdateSeries(UpdateSeriesRequestDto dto, [MappingTarget] SeriesModel model);
#pragma warning restore RMG012

	private partial SeriesMetadata ToSeriesMetadata(SeriesMetadataDto dto);

	private partial VolumeMetadata ToVolumeMetadata(VolumeMetadataDto dto);
}
