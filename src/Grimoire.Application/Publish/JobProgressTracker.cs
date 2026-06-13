namespace Grimoire.Application.Publish;

using Grimoire.Application.Publish.Dto;

public interface IJobProgressTracker
{
    void UpdateProgress(string jobId, int progress, string? stage = null);
    void CompleteJob(string jobId, string? downloadUrl = null);
    void FailJob(string jobId, string errorMessage);
}
