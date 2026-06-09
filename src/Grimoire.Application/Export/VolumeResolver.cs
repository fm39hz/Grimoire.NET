namespace Grimoire.Application.Export;

using System.Threading;
using Domain.Common;
using Domain.Entity.Book;
using Dto.Book;
using Service.Contract;

public class VolumeResolver(IBookTreeService bookTreeService) {
	public async Task<List<VolumeModel>> ResolveAsync(Guid seriesId, BinderyRequestDto request, CancellationToken cancellationToken = default) {
		var allVolumes = await bookTreeService.FindVolumes(seriesId, cancellationToken);
		var ordered = allVolumes.OrderBy(v => v.Order).ToList();

		if (string.Equals(request.Mode, "Single", StringComparison.OrdinalIgnoreCase)
			&& request.TargetVolumeIds is { Count: > 0 }) {
			var targetSet = request.TargetVolumeIds.ToHashSet();
			ordered = ordered
				.Where(v => targetSet.Contains(PrefixedId.ToString(EntityPrefix.Volume, v.Id)))
				.ToList();
		}

		return ordered;
	}
}
