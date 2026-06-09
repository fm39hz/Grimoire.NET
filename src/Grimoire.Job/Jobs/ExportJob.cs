using System;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Publish.Export;
using Grimoire.Application.Publish.Dto;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grimoire.Job.Jobs;

/// <summary>
///     Hangfire job that processes series export asynchronously.
///     Returns a JobResult — Hangfire stores it as "Result" in Succeeded state data.
/// </summary>
public sealed class ExportJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExportJob> _logger;

    public ExportJob(IServiceScopeFactory scopeFactory, ILogger<ExportJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 120)]
    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task<JobResult?> ExecuteAsync(
        PerformContext? context,
        Guid seriesId,
        BinderyRequestDto request,
        CancellationToken cancellationToken)
    {
        var jobId = context?.BackgroundJob.Id ?? Guid.NewGuid().ToString("N");

        _logger.LogInformation(
            "ExportJob started — JobId={JobId}, SeriesId={SeriesId}, Format={Format}, Mode={Mode}",
            jobId, seriesId, request.Format, request.Mode);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var services = scope.ServiceProvider;

            var pipeline = services.GetRequiredService<IExportPipeline>();
            var pipelineContext = new ExportPipelineContext(seriesId, request, jobId);

            await pipeline.ExecuteAsync(pipelineContext, cancellationToken);

            return pipelineContext.Result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("ExportJob cancelled — JobId={JobId}, SeriesId={SeriesId}", jobId, seriesId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExportJob crashed — JobId={JobId}, SeriesId={SeriesId}", jobId, seriesId);
            return JobResult.Fail(ex.Message);
        }
    }
}
