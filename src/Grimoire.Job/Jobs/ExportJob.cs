using System;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Publish;
using Grimoire.Application.Publish.Export;
using Grimoire.Application.Publish.Dto;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Grimoire.Job.Jobs;

public sealed class ExportJob : JobBase
{
    private Guid _seriesId;
    private BinderyRequestDto _request = null!;

    public ExportJob(IServiceScopeFactory scopeFactory, IJobProgressTracker progressTracker)
        : base(scopeFactory, progressTracker) { }

    [DisableConcurrentExecution(timeoutInSeconds: 120)]
    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task<JobResult?> ExecuteAsync(
        PerformContext? context,
        Guid seriesId,
        BinderyRequestDto request,
        CancellationToken cancellationToken)
    {
        _seriesId = seriesId;
        _request = request;
        return await ExecuteInternalAsync(context, nameof(ExportJob), cancellationToken);
    }

    protected override async Task<JobResult?> ExecuteCoreAsync(JobContext ctx, CancellationToken cancellationToken)
    {
        var services = ctx.Services;
        var pipeline = services.GetRequiredService<IExportPipeline>();

        var pipelineContext = new ExportPipelineContext(_seriesId, _request, ctx.JobId);
        pipelineContext.OnProgress = progress => ctx.Progress.Report(progress, pipelineContext.CurrentStage);

        await pipeline.ExecuteAsync(pipelineContext, cancellationToken);
        return pipelineContext.Result;
    }
}
