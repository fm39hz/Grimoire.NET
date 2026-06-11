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

	public void UpdateChapter(UpdateChapterRequestDto dto, ChapterModel model) {
		if (dto.Title is not null) {
			model.Title = dto.Title;
		}
		if (dto.Order is not null) {
			model.Order = dto.Order.Value;
		}
	}

	public void UpdateVolume(UpdateVolumeRequestDto dto, VolumeModel model) {
		if (dto.Title is not null) {
			model.Title = dto.Title;
		}
		if (dto.Order is not null) {
			model.Order = dto.Order.Value;
		}
		if (dto.Metadata is not null) {
			model.Metadata = MergeVolumeMetadata(model.Metadata, dto.Metadata);
		}
	}

	private static VolumeMetadata MergeVolumeMetadata(VolumeMetadata? existing, VolumeMetadataDto dto) {
		var current = existing ?? new VolumeMetadata();
		return new VolumeMetadata {
			CoverImage = dto.CoverImage ?? current.CoverImage,
			PublicationDate = dto.PublicationDate ?? current.PublicationDate,
			Isbn = dto.Isbn ?? current.Isbn
		};
	}

	public void UpdateSeries(UpdateSeriesRequestDto dto, SeriesModel model) {
		if (dto.Title is not null) {
			model.Title = dto.Title;
		}
		if (dto.Metadata is not null) {
			model.Metadata = MergeSeriesMetadata(model.Metadata, dto.Metadata);
		}
	}

	private SeriesMetadata MergeSeriesMetadata(SeriesMetadata existing, SeriesMetadataDto dto) {
		return new SeriesMetadata {
			Authors = dto.Authors ?? existing.Authors,
			Artists = dto.Artists ?? existing.Artists,
			Tags = dto.Tags ?? existing.Tags,
			Description = dto.Description != null 
				? dto.Description.Select(ToTextSegment).ToList() 
				: existing.Description,
			CoverImage = dto.CoverImage ?? existing.CoverImage
		};
	}

	public void MergeChapter(ChapterModel source, ChapterContentModel sourceContent, ChapterModel target) {
		target.Title = source.Title;
		target.Status = source.Status;
		
		if (target.ContentData != null) {
			target.ContentData.Segments = sourceContent.Segments;
			target.ContentData.Footnotes = sourceContent.Footnotes;
		} else {
			target.ContentData = new ChapterContentModel {
				Id = target.Id,
				Segments = sourceContent.Segments,
				Footnotes = sourceContent.Footnotes
			};
		}
	}

	private SeriesMetadata ToSeriesMetadata(SeriesMetadataDto dto) {
		return new SeriesMetadata {
			Authors = dto.Authors ?? [],
			Artists = dto.Artists ?? [],
			Tags = dto.Tags ?? [],
			Description = dto.Description != null
				? dto.Description.Select(ToTextSegment).ToList()
				: [],
			CoverImage = dto.CoverImage ?? string.Empty
		};
	}

	private static VolumeMetadata ToVolumeMetadata(VolumeMetadataDto dto) {
		return new VolumeMetadata {
			CoverImage = dto.CoverImage ?? string.Empty,
			PublicationDate = dto.PublicationDate,
			Isbn = dto.Isbn ?? string.Empty
		};
	}
}
