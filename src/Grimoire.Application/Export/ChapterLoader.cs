namespace Grimoire.Application.Export;

using System.Threading;
using Domain.Common.Repository;
using Domain.Entity.Book;

public class ChapterLoader(IChapterRepository chapterRepository) {
	public async Task<IReadOnlyDictionary<Guid, List<ChapterModel>>> LoadAsync(
		IEnumerable<VolumeModel> volumes,
		CancellationToken cancellationToken = default) {
		var volumeList = volumes as IList<VolumeModel> ?? volumes.ToList();
		if (volumeList.Count == 0) {
			return new Dictionary<Guid, List<ChapterModel>>();
		}

		var volumeIds = volumeList.Select(v => v.Id).ToList();
		var allChapters = await chapterRepository.FindByVolumeIdsWithContent(volumeIds, cancellationToken);

		return allChapters
			.GroupBy(c => c.VolumeId)
			.ToDictionary(g => g.Key, g => g.ToList());
	}
}
