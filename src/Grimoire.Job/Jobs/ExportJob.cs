using Grimoire.Application.Dto.Book;
using Grimoire.Application.Service.Contract;
using Grimoire.Domain.Common.Repository;
using Grimoire.Domain.Entity.Book;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;

namespace Grimoire.Job.Jobs;

/// <summary>
///     Hangfire job that processes series export asynchronously.
///     Result is stored as a regular AssetModel in the same storage as other assets.
///     Returns the asset ID as a plain string — Hangfire stores it as ReturnValue.
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

    /// <summary>Execute the export. Returns the asset ID on success, null on failure.</summary>
    [DisableConcurrentExecution(timeoutInSeconds: 120)]
    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task<string?> ExecuteAsync(
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

            // Dedup check: if source content hasn't changed since last export, skip building
            var prevRecord = await exportRecords.GetBySeriesAndFormatAsync(seriesId, formatDir, cancellationToken);
            if (prevRecord is not null)
            {
                var maxContentDt = await exportRecords.GetMaxContentTimestampAsync(seriesId, cancellationToken);
                if (prevRecord.LastExportedAt >= maxContentDt)
                {
                    _logger.LogInformation(
                        "Export skipped (unchanged) — JobId={JobId}, SeriesId={SeriesId}, Format={Format}",
                        jobId, seriesId, formatDir);
                    return prevRecord.AssetId.ToString();
                }
            }

            var exportResult = await bindery.ExportSeriesAsync(seriesId, request, cancellationToken);

            if (!exportResult.Success)
            {
                _logger.LogWarning(
                    "ExportJob failed — JobId={JobId}, Error={Error}",
                    jobId, exportResult.ErrorMessage);
                return null;
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

            return asset.Id.ToString();
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
