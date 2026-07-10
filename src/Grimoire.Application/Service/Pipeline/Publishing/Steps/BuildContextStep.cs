namespace Grimoire.Application.Service.Pipeline.Publishing.Steps;

using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Export;
using Grimoire.Domain.Common.Repository;
using Grimoire.Domain.Exception;

public sealed class BuildContextStep(
	ISeriesRepository seriesRepository,
	BookExportOrchestrator orchestrator) : IPublishingPipelineStep {

	public int ExecutionOrder => 10;

	public async Task ExecuteAsync(PublishingContext context, CancellationToken cancellationToken) {
		var series = await seriesRepository.FindOne(context.SeriesId, cancellationToken) ??
			throw new EntityNotFoundException($"Series with id {context.SeriesId} not found");

		var request = new BinderyRequestDto {
			Format = context.Format,
			Structure = context.Structure
		};

		context.ExportContext = await orchestrator.BuildContextAsync(series, request, cancellationToken);
	}
}
