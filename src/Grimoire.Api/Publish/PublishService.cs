namespace Grimoire.Api.Publish;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Publish;
using Grimoire.Application.Publish.Dto;
using Grimoire.Domain.Common.Repository;
using Grimoire.Job.Jobs;
using Hangfire;
using Hangfire.Common;

public sealed class PublishService : IPublishService
{
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly JobStorage _jobStorage;
    private readonly IStorageRepository _storage;

    public PublishService(
        IBackgroundJobClient backgroundJobs,
        JobStorage jobStorage,
        IStorageRepository storage)
    {
        _backgroundJobs = backgroundJobs;
        _jobStorage = jobStorage;
        _storage = storage;
    }

    public Task<string> EnqueueExportAsync(Guid seriesId, BinderyRequestDto request, CancellationToken cancellationToken = default)
    {
        var jobId = _backgroundJobs.Enqueue<ExportJob>(
            job => job.ExecuteAsync(
                null!, // PerformContext — filled by Hangfire
                seriesId,
                request,
                CancellationToken.None));

        return Task.FromResult(jobId);
    }

    public async Task<string> EnqueueImportAsync(
        CreateSeriesRequestDto seriesDto,
        List<ImportVolumeDto>? volumesOverride,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // Upload EPUB file to staging first
        var fileKey = await _storage.UploadFileAsync(
            fileStream,
            contentType,
            fileName,
            "staging/import",
            cancellationToken);

        var seriesJson = System.Text.Json.JsonSerializer.Serialize(seriesDto);
        var volumesJson = volumesOverride is not null ? System.Text.Json.JsonSerializer.Serialize(volumesOverride) : null;

        var jobId = _backgroundJobs.Enqueue<ImportJob>(
            job => job.ExecuteAsync(
                null!,
                seriesJson,
                volumesJson,
                fileKey,
                CancellationToken.None));

        return jobId;
    }

    public Task<PublishJobStatusDto> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var monitor = _jobStorage.GetMonitoringApi();
        var jobDetails = monitor.JobDetails(jobId);

        if (jobDetails is null)
        {
            return Task.FromResult(new PublishJobStatusDto(jobId, "NotFound"));
        }

        var state = jobDetails.History
            .Select(h => h.StateName)
            .FirstOrDefault() ?? "Unknown";

        if (state == "Succeeded")
        {
            return Task.FromResult(new PublishJobStatusDto(
                jobId,
                "Completed",
                DownloadUrl: $"/api/v1/publish/jobs/{jobId}/download"));
        }

        if (state == "Failed")
        {
            var error = jobDetails.History
                .Select(h => h.Data?.TryGetValue("ErrorMessage", out var msg) == true ? msg : null)
                .LastOrDefault(msg => msg != null) ?? "Unknown error";

            return Task.FromResult(new PublishJobStatusDto(jobId, "Failed", Error: error));
        }

        return Task.FromResult(new PublishJobStatusDto(jobId, state));
    }

    public async Task<PublishDownloadResultDto?> GetDownloadStreamAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var assetId = await ResolveAssetIdAsync(jobId);
        if (assetId is null)
        {
            return null;
        }

        var result = await _storage.GetFileStreamAsync(assetId.Value, cancellationToken);
        if (result is null)
        {
            return null;
        }

        return new PublishDownloadResultDto(result.Stream, result.ContentType, result.FileName);
    }

    private async Task<Guid?> ResolveAssetIdAsync(string jobId)
    {
        var monitor = _jobStorage.GetMonitoringApi();
        var jobDetails = monitor.JobDetails(jobId);

        if (jobDetails?.History is null) return null;

        var succeeded = jobDetails.History.FirstOrDefault(h => h.StateName == "Succeeded");
        if (succeeded?.Data is null) return null;

        if (!succeeded.Data.TryGetValue("Result", out var resultValue)
            || string.IsNullOrEmpty(resultValue))
        {
            return null;
        }

        try
        {
            // Using standard Hangfire helper or custom deserialization
            var result = SerializationHelper.Deserialize<JobResult>(resultValue);
            if (result?.DownloadUrl is not null && Guid.TryParse(result.DownloadUrl, out var assetId))
                return assetId;

            return null;
        }
        catch
        {
            return null;
        }
    }
}
