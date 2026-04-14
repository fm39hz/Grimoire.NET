namespace Grimoire.Application.Export;

using Domain.Common.Repository;
using Domain.Entity.Book;

public class ChapterLoader(IChapterRepository chapterRepository) {
	public async Task<IReadOnlyDictionary<Guid, List<ChapterModel>>> LoadAsync(
		IEnumerable<VolumeModel> volumes) {
		var volumeList = volumes as IList<VolumeModel> ?? volumes.ToList();
		if (volumeList.Count == 0) {
			return new Dictionary<Guid, List<ChapterModel>>();
		}

		var volumeIds = volumeList.Select(v => v.Id).ToList();
		var allChapters = await chapterRepository.FindByVolumeIdsWithContent(volumeIds);

		return allChapters
			.GroupBy(c => c.VolumeId)
			.ToDictionary(g => g.Key, g => g.ToList());
	}
}
