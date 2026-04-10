namespace Grimoire.Application.Mapper;

using Domain.Entity;
using Domain.Entity.Book;
using Domain.Entity.Book.Metadata;
using Domain.Entity.Book.Segment;
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
		Metadata = dto.Metadata != null? ToVolumeMetadata(dto.Metadata) : null
	};

	public ChapterModel CreateChapter(CreateChapterRequestDto dto, Guid volumeId) {
		var idMap = new Dictionary<string, Guid>();
		var cleanFootnotes = new List<FootnoteSegmentModel>();

		if (dto.Footnotes != null) {
			foreach (var note in dto.Footnotes) {
				if (note == null || string.IsNullOrEmpty(note.InitialId)) {
					continue;
				}

				var systemId = Guid.CreateVersion7();

				idMap[note.InitialId] = systemId;
				cleanFootnotes.Add(new FootnoteSegmentModel { Id = systemId, Segments = note.Segments });
			}
		}

		var cleanContent = new List<SegmentModel>();

		if (dto.Content != null) {
			foreach (var segment in dto.Content) {
				if (segment is TextSegmentModel textSeg) {
					var updatedRuns = textSeg.Runs.Select(run => !string.IsNullOrEmpty(run.FootnoteId) &&
																idMap.TryGetValue(run.FootnoteId, out var systemId)
						? run with { FootnoteId = systemId.ToString() }
						: run).ToList();

					cleanContent.Add(textSeg with { Runs = updatedRuns });
				}
				else {
					cleanContent.Add(segment);
				}
			}
		}

		var chapterId = Guid.CreateVersion7();
		return new ChapterModel {
			Id = chapterId,
			VolumeId = volumeId,
			Order = dto.Order,
			Title = dto.Title,
			ContentData = new ChapterContentModel {
				Id = chapterId, Segments = cleanContent, Footnotes = cleanFootnotes
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
