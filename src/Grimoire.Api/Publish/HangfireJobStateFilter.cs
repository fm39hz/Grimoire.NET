namespace Grimoire.Api.Publish;

using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Grimoire.Application.Publish;
using Grimoire.Application.Publish.Dto;

public sealed class HangfireJobStateFilter : IApplyStateFilter
{
    private readonly IJobProgressTracker _progressTracker;

    public HangfireJobStateFilter(IJobProgressTracker progressTracker)
    {
        _progressTracker = progressTracker;
    }

    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        var jobId = context.BackgroundJob.Id;
        var stateName = context.NewState.Name;

        if (stateName == SucceededState.StateName)
        {
            string? downloadUrl = null;
            if (context.NewState is SucceededState succeededState && succeededState.Result is JobResult jobResult)
            {
                if (!jobResult.Success)
                {
                    _progressTracker.FailJob(jobId, jobResult.ErrorMessage ?? "Job execution failed");
                    return;
                }
                downloadUrl = jobResult.DownloadUrl;
            }
            _progressTracker.CompleteJob(jobId, downloadUrl);
        }
        else if (stateName == FailedState.StateName)
        {
            var exception = (context.NewState as FailedState)?.Exception;
            var errorMessage = exception?.Message ?? "Job execution failed";
            _progressTracker.FailJob(jobId, errorMessage);
        }
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        // No-op
    }
}
