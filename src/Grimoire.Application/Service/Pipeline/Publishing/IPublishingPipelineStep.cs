namespace Grimoire.Application.Service.Pipeline.Publishing;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
///     Represents a single step in the Publishing pipeline
/// </summary>
public interface IPublishingPipelineStep {
	/// <summary>
	///     Defines the execution order of the step in the pipeline (lower values run first)
	/// </summary>
	public int ExecutionOrder { get; }

	/// <summary>
	///     Executes the business logic of this publishing step
	/// </summary>
	public Task ExecuteAsync(PublishingContext context, CancellationToken cancellationToken);
}
