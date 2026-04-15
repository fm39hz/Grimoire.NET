namespace Grimoire.Application.Export;

using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Dto.Book;

public class VolumeResolver(IVolumeRepository volumeRepository) {
	public async Task<List<VolumeModel>> ResolveAsync(Guid seriesId, BinderyRequestDto request) {
		var allVolumes = await volumeRepository.FindBySeriesId(seriesId);
		var ordered = allVolumes.OrderBy(v => v.Order).ToList();

		if (request.Mode.Equals("Single", StringComparison.OrdinalIgnoreCase)
			&& request.TargetVolumeIds is { Count: > 0 }) {
			var targetSet = request.TargetVolumeIds.ToHashSet();
			ordered = ordered
				.Where(v => targetSet.Contains(PrefixedId.ToString(EntityPrefix.Volume, v.Id)))
				.ToList();
		}

		return ordered;
	}
}
