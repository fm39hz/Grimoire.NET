namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Domain.Exception;
using Dto.Book;
using System.Collections.Generic; // Added for ICollection

public sealed class ChapterService(IChapterRepository repository) : IChapterService {
	public async Task<ChapterModel?> FindOne(Guid id) => await repository.FindOne(id);

	public async Task<IEnumerable<ChapterModel>> FindAll() => await repository.FindAll();

	public async Task<ChapterModel> Create(CreateChapterRequestDto dto) {
		var chapter = new ChapterModel {
			VolumeId = dto.VolumeId,
			Order = dto.Order,
			Title = dto.Title,
			Variants = new List<ChapterVariantModel>() // Initialize the collection
		};

		foreach (var variantDto in dto.Variants) {
			var idMap = new Dictionary<string, Guid>();
			var cleanFootnotes = new List<FootnoteSegmentModel>();

			if (variantDto.Footnotes != null) {
				foreach (var note in variantDto.Footnotes) {
					var systemId = Guid.CreateVersion7();

					if (note == null || string.IsNullOrEmpty(note.InitialId)) {
						continue;
					}

					idMap[note.InitialId] = systemId;
					cleanFootnotes.Add(new FootnoteSegmentModel { Id = systemId, Segments = note.Segments });
				}
			}

			var cleanContent = new List<SegmentModel>();
			foreach (var segment in variantDto.Content) {
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

			chapter.Variants.Add(new ChapterVariantModel {
				ChapterId = chapter.Id, // Set the required ChapterId
				Type = variantDto.Type,
				Language = variantDto.Language,
				SourceName = variantDto.SourceName,
				Content = cleanContent,
				Footnotes = cleanFootnotes
			});
		}

		return await repository.Create(chapter);
	}

	public async Task<ChapterModel> CreateFromImportAsync(CreateChapterRequestDto dto) => await Create(dto);

	public async Task<ChapterModel> Update(Guid id, UpdateChapterRequestDto dto) {
		var chapter = await repository.FindOne(id) ??
					throw new EntityNotFoundException($"Chapter with id {id} not found");
		chapter.Order = dto.Order ?? chapter.Order;
		chapter.Title = dto.Title ?? chapter.Title;

		if (dto.Variants is null) {
			return await repository.Update(chapter);
		}

		// Clear existing variants and add new ones from DTO
		chapter.Variants.Clear(); // Assuming update means replacing all variants

		foreach (var variantDto in dto.Variants) {
			var idMap = new Dictionary<string, Guid>();
			var cleanFootnotes = new List<FootnoteSegmentModel>();

			if (variantDto.Footnotes != null) {
				foreach (var note in variantDto.Footnotes) {
					var systemId = Guid.CreateVersion7();

					if (note == null || string.IsNullOrEmpty(note.InitialId)) {
						continue;
					}

					idMap[note.InitialId] = systemId;
					cleanFootnotes.Add(new FootnoteSegmentModel { Id = systemId, Segments = note.Segments });
				}
			}

			var cleanContent = new List<SegmentModel>();
			foreach (var segment in variantDto.Content) {
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

			chapter.Variants.Add(new ChapterVariantModel {
				ChapterId = chapter.Id,
				Type = variantDto.Type,
				Language = variantDto.Language,
				SourceName = variantDto.SourceName,
				Content = cleanContent,
				Footnotes = cleanFootnotes
			});
		}

		return await repository.Update(chapter);
	}

	public async Task<int> Delete(Guid id) => await repository.Delete(id);

    public async Task<IEnumerable<ChapterVariantResponseDto>> GetVariantsByIdsAsync(IEnumerable<Guid> ids) {
        var variants = await repository.FindVariantsByIdsAsync(ids);
        return variants.Select(v => new ChapterVariantResponseDto(v));
    }

    public async Task<IEnumerable<ChapterVariantResponseDto>> GetVariantsByChapterIdAsync(Guid chapterId, VariantType[]? types) {
        var variants = await repository.FindVariantsByChapterIdAsync(chapterId);
        if (types is not null && types.Length > 0) {
            variants = variants.Where(v => types.Contains(v.Type));
        }
        return variants.Select(v => new ChapterVariantResponseDto(v));
    }
}
