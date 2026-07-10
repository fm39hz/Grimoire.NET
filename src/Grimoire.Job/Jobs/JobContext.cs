namespace Grimoire.Job.Jobs;

using Microsoft.Extensions.Logging;

public sealed class JobContext(string jobId, JobProgressWriter progress, ILogger logger) {
	public string JobId { get; } = jobId;
	public JobProgressWriter Progress { get; } = progress;
	public IServiceProvider Services { get; internal set; } = null!;
	public ILogger Logger { get; } = logger;
}
