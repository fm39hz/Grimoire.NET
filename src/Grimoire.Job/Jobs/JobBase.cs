namespace Grimoire.Job.Jobs;

using Grimoire.Application.Publish;
using Grimoire.Application.Publish.Dto;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public abstract class JobBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IJobProgressTracker _progressTracker;

    protected JobBase(IServiceScopeFactory scopeFactory, IJobProgressTracker progressTracker)
    {
        _scopeFactory = scopeFactory;
        _progressTracker = progressTracker;
    }

    /// <summary>Override with job-specific logic. Scope + Services already set on ctx.</summary>
    protected abstract Task<JobResult?> ExecuteCoreAsync(JobContext ctx, CancellationToken cancellationToken);

    protected async Task<JobResult?> ExecuteInternalAsync(
        PerformContext? context,
        string jobName,
        CancellationToken cancellationToken)
    {
        var jobId = context?.BackgroundJob.Id ?? Guid.NewGuid().ToString("N");
        using var scope = _scopeFactory.CreateScope();

        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(jobName);
        var writer = new JobProgressWriter(_progressTracker, jobId);
        var ctx = new JobContext(jobId, writer, logger) { Services = scope.ServiceProvider };

        logger.LogInformation("{JobName} started — JobId={JobId}", jobName, jobId);

        try
        {
            var result = await ExecuteCoreAsync(ctx, cancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("{JobName} cancelled — JobId={JobId}", jobName, jobId);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{JobName} crashed — JobId={JobId}", jobName, jobId);
            return JobResult.Fail(ex.Message);
        }
    }
}
