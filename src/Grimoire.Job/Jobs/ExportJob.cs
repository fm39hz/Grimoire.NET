using Grimoire.Application.Dto.Book;
using Grimoire.Application.Service.Contract;
using Grimoire.Domain.Common.Repository;
using Grimoire.Domain.Entity.Book;
using Grimoire.Job.Common;
using Hangfire;
using Hangfire.Server;
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

            var bindery = services.GetRequiredService<IBinderyService>();
            var storage = services.GetRequiredService<IStorageRepository>();
            var exportRecords = services.GetRequiredService<ISeriesExportRecordRepository>();

            var formatDir = request.Format.ToString().ToLowerInvariant();

            // Dedup check: if source content unchanged, return existing asset
            var prevRecord = await exportRecords.GetBySeriesAndFormatAsync(seriesId, formatDir, cancellationToken);
            if (prevRecord is not null)
            {
                var maxContentDt = await exportRecords.GetMaxContentTimestampAsync(seriesId, cancellationToken);
                if (prevRecord.LastExportedAt >= maxContentDt)
                {
                    _logger.LogInformation(
                        "Export skipped (unchanged) — JobId={JobId}, SeriesId={SeriesId}, Format={Format}",
                        jobId, seriesId, formatDir);
                    return JobResult.Ok(prevRecord.AssetId.ToString(), "", "");
                }
            }

            var exportResult = await bindery.ExportSeriesAsync(seriesId, request, cancellationToken);

            if (!exportResult.Success)
            {
                _logger.LogWarning(
                    "ExportJob failed — JobId={JobId}, Error={Error}",
                    jobId, exportResult.ErrorMessage);
                return JobResult.Fail(exportResult.ErrorMessage);
            }

            var asset = await storage.UploadAssetAsync(
                seriesId,
                exportResult.ContentStream,
                exportResult.ContentType,
                exportResult.FileName,
                AssetRefType.Export,
                prefix: $"staging/export/{formatDir}",
                cancellationToken);

            // Update export record
            if (prevRecord is not null)
            {
                prevRecord.LastExportedAt = DateTime.UtcNow;
                prevRecord.AssetId = asset.Id;
                await exportRecords.Update(prevRecord, cancellationToken);
            }
            else
            {
                await exportRecords.Create(new SeriesExportRecord
                {
                    SeriesId = seriesId,
                    Format = formatDir,
                    LastExportedAt = DateTime.UtcNow,
                    AssetId = asset.Id
                }, cancellationToken);
            }

            _logger.LogInformation(
                "ExportJob completed — JobId={JobId}, SeriesId={SeriesId}, AssetId={AssetId}",
                jobId, seriesId, asset.Id);

            return JobResult.Ok(asset.Id.ToString(), exportResult.FileName, exportResult.ContentType);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("ExportJob cancelled — JobId={JobId}, SeriesId={SeriesId}", jobId, seriesId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExportJob crashed — JobId={JobId}, SeriesId={SeriesId}", jobId, seriesId);
            return null;
        }
    }
}
