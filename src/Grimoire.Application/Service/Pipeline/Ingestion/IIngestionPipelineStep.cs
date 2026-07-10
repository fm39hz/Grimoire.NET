namespace Grimoire.Application.Service.Pipeline.Ingestion;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
///     Represents a single step in the Ingestion pipeline
/// </summary>
public interface IIngestionPipelineStep {
	/// <summary>
	///     Defines the execution order of the step in the pipeline (lower values run first)
	/// </summary>
	public int ExecutionOrder { get; }

	/// <summary>
	///     Executes the business logic of this ingestion step
	/// </summary>
	public Task ExecuteAsync(IngestionContext context, CancellationToken cancellationToken);
}
