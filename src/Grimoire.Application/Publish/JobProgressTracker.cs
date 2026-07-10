namespace Grimoire.Application.Publish;

public interface IJobProgressTracker {
	public void UpdateProgress(string jobId, int progress, string? stage = null);
	public void CompleteJob(string jobId, string? downloadUrl = null);
	public void FailJob(string jobId, string errorMessage);
}
