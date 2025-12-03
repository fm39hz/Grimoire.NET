namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;

public sealed class ChapterService(IChapterRepository repository) : IChapterService {
	public async Task<ChapterModel?> FindOne(Guid id) => await repository.FindOne(id);

	public async Task<IEnumerable<ChapterModel>> FindAll() => await repository.FindAll();

	public async Task<ChapterModel> Create(ChapterModel entity) => await repository.Create(entity);

	public async Task<ChapterModel> CreateFromImportAsync(ChapterRequestDto dto) {
		var idMap = new Dictionary<string, Guid>();
		var cleanFootnotes = new List<FootnoteSegmentModel>();

		if (dto.Footnotes != null) {
			foreach (var note in dto.Footnotes) {
				var systemId = Guid.CreateVersion7();

				if (note == null || string.IsNullOrEmpty(note.InitialId)) {
					continue;
				}

				idMap[note.InitialId] = systemId;
				cleanFootnotes.Add(new FootnoteSegmentModel { Id = systemId, Segments = note.Segments });
			}
		}

		var cleanContent = new List<SegmentModel>();
		foreach (var segment in dto.Content) {
			if (segment is TextSegmentModel textSeg) {
				var updatedRuns = textSeg.Runs.Select(run => {
					if (!string.IsNullOrEmpty(run.FootnoteId) &&
						idMap.TryGetValue(run.FootnoteId, out Guid systemId)) {
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

		var chapter = new ChapterModel {
			VolumeId = dto.VolumeId,
			Order = dto.Order,
			Title = dto.Title,
			Content = cleanContent,
			Footnotes = cleanFootnotes
		};

		return await repository.Create(chapter);
	}

	public async Task<ChapterModel> Update(Guid id, ChapterModel entity) {
		var chapter = new ChapterModel {
			Id = id,
			VolumeId = entity.VolumeId,
			Order = entity.Order,
			Title = entity.Title,
			Content = entity.Content
		};

		return await repository.Update(chapter);
	}

	public async Task<int> Delete(Guid id) => await repository.Delete(id);
}
