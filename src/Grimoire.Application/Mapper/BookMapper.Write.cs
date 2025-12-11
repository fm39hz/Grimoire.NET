namespace Grimoire.Application.Mapper;

using Domain.Entity;
using Domain.Entity.Book;
using Domain.Entity.Book.Metadata;
using Domain.Entity.Book.Segment;
using Dto.Book;
using Dto.Book.Metadata;
using Riok.Mapperly.Abstractions;
using DomainCommon = Domain.Common;

public partial class BookMapper {
#pragma warning disable RMG012
	[MapperIgnoreTarget(nameof(BaseModel.CreatedAt))]
	[MapperIgnoreTarget(nameof(BaseModel.UpdatedAt))]
	[MapperIgnoreTarget(nameof(BaseModel.Id))]
	[MapperIgnoreTarget(nameof(SeriesModel.GlossaryTerms))]
	[MapperIgnoreTarget(nameof(SeriesModel.SourceMaterials))]
	public partial SeriesModel CreateSeries(CreateSeriesRequestDto dto);
#pragma warning restore RMG012

	[MapperIgnoreTarget(nameof(BaseModel.CreatedAt))]
	[MapperIgnoreTarget(nameof(BaseModel.UpdatedAt))]
	[MapperIgnoreTarget(nameof(BaseModel.Id))]
	[MapProperty(nameof(CreateVolumeRequestDto.SeriesId), nameof(VolumeModel.SeriesId),
		Use = nameof(ParseStringToGuid))]
	public partial VolumeModel CreateVolume(CreateVolumeRequestDto dto);

	public ChapterModel CreateChapter(CreateChapterRequestDto dto) {
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
					var updatedRuns = textSeg.Runs.Select(run => {
						if (!string.IsNullOrEmpty(run.FootnoteId) &&
							idMap.TryGetValue(run.FootnoteId, out var systemId)) {
							return run with { FootnoteId = systemId.ToString() };
						}

						return run;
					}).ToList();

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
			VolumeId = DomainCommon.PrefixedId.ToGuid(dto.VolumeId, DomainCommon.EntityPrefix.Volume),
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
	[MapperIgnoreTarget(nameof(SeriesModel.GlossaryTerms))]
	[MapperIgnoreTarget(nameof(SeriesModel.SourceMaterials))]
	public partial void UpdateSeries(UpdateSeriesRequestDto dto, [MappingTarget] SeriesModel model);
#pragma warning restore RMG012

	private partial SeriesMetadata ToSeriesMetadata(SeriesMetadataDto dto);

	private partial VolumeMetadata ToVolumeMetadata(VolumeMetadataDto dto);

	// Helper for parsing string IDs to Guid
	private static Guid ParseStringToGuid(string prefixedId) => DomainCommon.PrefixedId.ToGuid(prefixedId, DomainCommon.EntityPrefix.Series);
}
