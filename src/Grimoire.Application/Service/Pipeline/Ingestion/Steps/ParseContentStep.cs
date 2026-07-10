namespace Grimoire.Application.Service.Pipeline.Ingestion.Steps;

using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Service.Strategy;

public sealed class ParseContentStep(IIngestionStrategyFactory strategyFactory) : IIngestionPipelineStep {
	public int ExecutionOrder => 10;

	public async Task ExecuteAsync(IngestionContext context, CancellationToken cancellationToken) {
		var strategy = strategyFactory.GetStrategy(context.RequestDto);
		var result = await strategy.ExecuteAsync(context.RequestDto, context.VolumeId, cancellationToken);

		context.Chapter = result.Chapter;
		context.Segments.AddRange(result.Segments);
		context.SourceMaterial = result.Source;
	}
}
