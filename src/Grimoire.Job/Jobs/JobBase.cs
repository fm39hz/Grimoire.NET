namespace Grimoire.Job.Jobs;

using Grimoire.Application.Publish;
using Grimoire.Application.Publish.Dto;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public abstract partial class JobBase(IServiceScopeFactory scopeFactory, IJobProgressTracker progressTracker) {
	private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
	private readonly IJobProgressTracker _progressTracker = progressTracker;

	/// <summary>Override with job-specific logic. Scope + Services already set on ctx.</summary>
	protected abstract Task<JobResult?> ExecuteCoreAsync(JobContext ctx, CancellationToken cancellationToken);

	protected async Task<JobResult?> ExecuteInternalAsync(
		PerformContext? context,
		string jobName,
		CancellationToken cancellationToken) {
		var jobId = context?.BackgroundJob.Id ?? Guid.NewGuid().ToString("N");
		using var scope = _scopeFactory.CreateScope();

		var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
		var logger = loggerFactory.CreateLogger(jobName);
		var writer = new JobProgressWriter(_progressTracker, jobId);
		var ctx = new JobContext(jobId, writer, logger) { Services = scope.ServiceProvider };

		LogJobStarted(logger, jobName, jobId);

		try {
			var result = await ExecuteCoreAsync(ctx, cancellationToken);
			return result;
		}
		catch (OperationCanceledException) {
			LogJobCancelled(logger, jobName, jobId);
			return null;
		}
		catch (Exception ex) {
			LogJobCrashed(logger, ex, jobName, jobId);
			return JobResult.Fail(ex.Message);
		}
	}

	[LoggerMessage(LogLevel.Information, "{JobName} started — JobId={JobId}")]
	private static partial void LogJobStarted(ILogger logger, string jobName, string jobId);

	[LoggerMessage(LogLevel.Warning, "{JobName} cancelled — JobId={JobId}")]
	private static partial void LogJobCancelled(ILogger logger, string jobName, string jobId);

	[LoggerMessage(LogLevel.Error, "{JobName} crashed — JobId={JobId}")]
	private static partial void LogJobCrashed(ILogger logger, Exception ex, string jobName, string jobId);
}
