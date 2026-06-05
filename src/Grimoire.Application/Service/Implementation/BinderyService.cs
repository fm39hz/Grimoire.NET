namespace Grimoire.Application.Service.Implementation;

using System.Threading;
using Contract;
using Domain.Common.Repository;
using Domain.Exception;
using Dto.Book;
using Export;
using Strategy;

public sealed class BinderyService(
	ISeriesRepository seriesRepository,
	BookExportOrchestrator orchestrator,
	IEnumerable<IExportStrategy> exportStrategies
	)
	: IBinderyService {
	public async Task<ExportResult> ExportSeriesAsync(Guid seriesId, BinderyRequestDto request, CancellationToken cancellationToken = default) {
		var series = await seriesRepository.FindOne(seriesId, cancellationToken) ??
					throw new EntityNotFoundException($"Series with id {seriesId} not found");
		var structure = request.Structure ?? ExportStructureDefaults.Standard();
		var context = await orchestrator.BuildContextAsync(series, request with { Structure = structure }, cancellationToken);
		var strategy = exportStrategies.FirstOrDefault(s => s.Format == request.Format) ??
						throw new InvalidOperationException($"No export strategy found for format: {request.Format}");

		return await strategy.ExportAsync(context, cancellationToken);
	}
}
