namespace Grimoire.Job.Jobs;

using Grimoire.Application.Dto.Book;
using Grimoire.Application.Publish;
using Grimoire.Application.Publish.Dto;
using Grimoire.Application.Publish.Export;
using Hangfire;
using Hangfire.Server;

public sealed class ExportJob(IServiceScopeFactory scopeFactory, IJobProgressTracker progressTracker) : JobBase(scopeFactory, progressTracker) {
	private Guid _seriesId;
	private BinderyRequestDto _request = null!;

	[DisableConcurrentExecution(timeoutInSeconds: 120)]
	[AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
	public async Task<JobResult?> ExecuteAsync(
		PerformContext? context,
		Guid seriesId,
		BinderyRequestDto request,
		CancellationToken cancellationToken) {
		_seriesId = seriesId;
		_request = request;
		return await ExecuteInternalAsync(context, nameof(ExportJob), cancellationToken);
	}

	protected override async Task<JobResult?> ExecuteCoreAsync(JobContext ctx, CancellationToken cancellationToken) {
		var services = ctx.Services;
		var pipeline = services.GetRequiredService<IExportPipeline>();

		var pipelineContext = new ExportPipelineContext(_seriesId, _request, ctx.JobId);
		pipelineContext.OnProgress = progress => ctx.Progress.Report(progress, pipelineContext.CurrentStage);

		await pipeline.ExecuteAsync(pipelineContext, cancellationToken);
		return pipelineContext.Result;
	}
}
