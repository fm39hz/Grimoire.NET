namespace Grimoire.Job.Jobs;

using Microsoft.Extensions.Logging;

public sealed class JobContext
{
    public string JobId { get; }
    public JobProgressWriter Progress { get; }
    public IServiceProvider Services { get; internal set; } = null!;
    public ILogger Logger { get; }

    public JobContext(string jobId, JobProgressWriter progress, ILogger logger)
    {
        JobId = jobId;
        Progress = progress;
        Logger = logger;
    }
}
