namespace Grimoire.Job.Jobs;

using Grimoire.Application.Publish;
using Hangfire;

public sealed class JobProgressWriter
{
    private readonly IJobProgressTracker _tracker;
    private readonly string _jobId;
    private int _lastDbProgress = -1;
    private DateTime _lastDbWrite = DateTime.MinValue;
    private string? _lastStage;

    public JobProgressWriter(IJobProgressTracker tracker, string jobId)
    {
        _tracker = tracker;
        _jobId = jobId;
    }

    public void Report(int progress, string? stage = null)
    {
        _tracker.UpdateProgress(_jobId, progress, stage);

        if (progress == _lastDbProgress && stage == _lastStage)
            return;

        if (Math.Abs(progress - _lastDbProgress) < 1 && DateTime.UtcNow - _lastDbWrite <= TimeSpan.FromMilliseconds(500))
            return;

        try
        {
            using var connection = JobStorage.Current.GetConnection();
            connection.SetJobParameter(_jobId, "Progress", progress.ToString());
            if (stage is not null && stage != _lastStage)
                connection.SetJobParameter(_jobId, "Stage", stage);
            _lastDbProgress = progress;
            _lastStage = stage;
            _lastDbWrite = DateTime.UtcNow;
        }
        catch
        {
            // Suppress — don't let progress reporting break the job
        }
    }
}
