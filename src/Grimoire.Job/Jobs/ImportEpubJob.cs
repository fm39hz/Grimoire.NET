using System.Text.Json;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Import;
using Grimoire.Domain.Common.Repository;
using Grimoire.Job.Common;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;

namespace Grimoire.Job.Jobs;

public sealed class ImportEpubJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ImportEpubJob> _logger;

    public ImportEpubJob(IServiceScopeFactory scopeFactory, ILogger<ImportEpubJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    [AutomaticRetry(Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task<JobResult?> ExecuteAsync(
        PerformContext? context,
        string seriesDtoJson,
        string? volumesJson,
        string fileKey,
        CancellationToken cancellationToken)
    {
        var jobId = context?.BackgroundJob.Id ?? Guid.NewGuid().ToString("N");

        _logger.LogInformation(
            "ImportEpubJob started — JobId={JobId}, FileKey={FileKey}",
            jobId, fileKey);

        try
        {
            var seriesDto = JsonSerializer.Deserialize<CreateSeriesRequestDto>(seriesDtoJson)
                ?? throw new InvalidOperationException("Failed to deserialize series DTO");

            List<ImportVolumeDto>? volumesOverride = null;
            if (!string.IsNullOrEmpty(volumesJson))
                volumesOverride = JsonSerializer.Deserialize<List<ImportVolumeDto>>(volumesJson);

            using var scope = _scopeFactory.CreateScope();
            var services = scope.ServiceProvider;

            var strategyFactory = services.GetRequiredService<ImportStrategyFactory>();
            var strategy = strategyFactory.GetStrategy(fileKey);

            var orchestrator = services.GetRequiredService<IImportOrchestrator>();
            var storage = services.GetRequiredService<IStorageRepository>();

            await using var sourceStream = await storage.GetFileByPathAsync(fileKey, cancellationToken)
                ?? throw new InvalidOperationException($"Source file not found: {fileKey}");

            var result = await orchestrator.ImportAsync(
                strategy, seriesDto, volumesOverride, sourceStream, cancellationToken);

            _logger.LogInformation(
                "ImportEpubJob completed — JobId={JobId}, SeriesId={SeriesId}, " +
                "VolumesCreated={VolumesCreated}, VolumesUpdated={VolumesUpdated}, " +
                "ChaptersCreated={ChaptersCreated}, ChaptersUpdated={ChaptersUpdated}",
                jobId, result.SeriesId,
                result.VolumesCreated, result.VolumesUpdated,
                result.ChaptersCreated, result.ChaptersUpdated);

            return JobResult.Ok(result.SeriesId.ToString(), "import-completed", "application/json");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("ImportEpubJob cancelled — JobId={JobId}", jobId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ImportEpubJob crashed — JobId={JobId}", jobId);
            return null;
        }
    }
}
