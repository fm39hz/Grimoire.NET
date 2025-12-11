namespace Grimoire.Application.Service.Implementation;

using Common;
using Contract;
using DomainCommon = Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Exception;
using Dto.Book;
using Strategy;

public sealed class BinderyService(
	ISeriesRepository seriesRepository,
	IVolumeRepository volumeRepository,
	IEnumerable<IExportStrategy> exportStrategies)
	: IBinderyService {
	public async Task<ExportResult> ExportSeriesAsync(Guid seriesId, BinderyRequestDto request) {
		var series = await seriesRepository.FindOne(seriesId) ??
					throw new EntityNotFoundException($"Series with id {seriesId} not found");

		var volumes = request.Mode == "Single" && request.TargetVolumeIds != null
			? await GetSpecificVolumes(seriesId, request.TargetVolumeIds)
			: await volumeRepository.FindBySeriesId(seriesId);

		var strategy = exportStrategies.FirstOrDefault(s => s.Format == request.Format) ??
						throw new InvalidOperationException($"No export strategy found for format: {request.Format}");

		return await strategy.ExportAsync(series, volumes, request);
	}

	private async Task<IEnumerable<VolumeModel>> GetSpecificVolumes(Guid seriesId, List<string> volumeIds) {
		var volumes = new List<VolumeModel>();
		foreach (var volumeId in volumeIds) {
			try {
				var guid = DomainCommon.PrefixedId.ToGuid(volumeId, DomainCommon.EntityPrefix.Volume);
				var volume = await volumeRepository.FindOne(guid);
				if (volume != null && volume.SeriesId == seriesId) {
					volumes.Add(volume);
				}
			}
			catch (FormatException) {
				// Skip invalid volume IDs instead of failing entire operation
				continue;
			}
		}

		return volumes;
	}
}
