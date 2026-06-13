using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Import;
using Grimoire.Application.Publish.Import;
using Grimoire.Domain.Common.Repository;
using Grimoire.Application.Publish.Dto;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Grimoire.Infrastructure.Configuration;
using Grimoire.Application.Publish;

namespace Grimoire.Job.Jobs;

public sealed class ImportJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ImportJob> _logger;
    private readonly IJobProgressTracker _progressTracker;

    public ImportJob(IServiceScopeFactory scopeFactory, ILogger<ImportJob> logger, IJobProgressTracker progressTracker)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _progressTracker = progressTracker;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    [AutomaticRetry(Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task<JobResult?> ExecuteAsync(
        PerformContext? context,
        string? seriesDtoJson,
        string? volumesJson,
        string fileKey,
        CancellationToken cancellationToken)
    {
        var jobId = context?.BackgroundJob.Id ?? Guid.NewGuid().ToString("N");

        _logger.LogInformation(
            "ImportJob started — JobId={JobId}, FileKey={FileKey}",
            jobId, fileKey);

        try
        {
            CreateSeriesRequestDto? seriesDto = null;
            if (!string.IsNullOrEmpty(seriesDtoJson))
            {
                seriesDto = JsonSerializer.Deserialize<CreateSeriesRequestDto>(seriesDtoJson, JsonConfiguration.JsonOptions);
            }

            List<ImportVolumeDto>? volumesOverride = null;
            if (!string.IsNullOrEmpty(volumesJson))
                volumesOverride = JsonSerializer.Deserialize<List<ImportVolumeDto>>(volumesJson, JsonConfiguration.JsonOptions);

            using var scope = _scopeFactory.CreateScope();
            var services = scope.ServiceProvider;

            var strategyFactory = services.GetRequiredService<ImportStrategyFactory>();
            var strategy = strategyFactory.GetStrategy(fileKey);

            var storage = services.GetRequiredService<IStorageRepository>();
            await using var sourceStream = await storage.GetFileByPathAsync(fileKey, cancellationToken)
                ?? throw new InvalidOperationException($"Source file not found: {fileKey}");

            var pipeline = services.GetRequiredService<IImportPipeline>();
            int lastDbProgress = -1;
            var lastDbWrite = DateTime.MinValue;

            var pipelineContext = new ImportPipelineContext(strategy, seriesDto, volumesOverride, sourceStream, jobId);
            pipelineContext.OnProgress = progress =>
            {
                _progressTracker.UpdateProgress(jobId, progress, pipelineContext.CurrentStage);

                    if (progress != lastDbProgress && (Math.Abs(progress - lastDbProgress) >= 1 || DateTime.UtcNow - lastDbWrite > TimeSpan.FromMilliseconds(500)))
                    {
                        try
                        {
                            using var connection = JobStorage.Current.GetConnection();
                            connection.SetJobParameter(jobId, "Progress", progress.ToString());
                            if (pipelineContext.CurrentStage is not null)
                                connection.SetJobParameter(jobId, "Stage", pipelineContext.CurrentStage);
                            lastDbProgress = progress;
                            lastDbWrite = DateTime.UtcNow;
                        }
                        catch
                        {
                            // Suppress progress reporting errors to not block the main pipeline
                        }
                    }
            };

            await pipeline.ExecuteAsync(pipelineContext, cancellationToken);

            if (pipelineContext.Result is not null)
            {
                _logger.LogInformation(
                    "ImportJob completed — JobId={JobId}, SeriesId={SeriesId}, " +
                    "VolumesCreated={VolumesCreated}, VolumesUpdated={VolumesUpdated}, " +
                    "ChaptersCreated={ChaptersCreated}, ChaptersUpdated={ChaptersUpdated}",
                    jobId, pipelineContext.Series?.Id,
                    pipelineContext.ResolvedVolumes.Count(v => v.WasCreated),
                    pipelineContext.ResolvedVolumes.Count(v => !v.WasCreated),
                    pipelineContext.ChaptersCreated, pipelineContext.ChaptersUpdated);
            }

            return pipelineContext.Result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("ImportJob cancelled — JobId={JobId}", jobId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ImportJob crashed — JobId={JobId}", jobId);
            return JobResult.Fail(ex.Message);
        }
    }
}
