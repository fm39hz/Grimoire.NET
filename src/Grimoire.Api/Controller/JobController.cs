using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Grimoire.Api.Constant;
using Grimoire.Domain.Common.Repository;
using Grimoire.Job.Common;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Api.Controller;

/// <summary>
///     Monitor background job status and download results.
///     Export files are stored as regular AssetModel entries via IStorageRepository.
///     The asset ID is read from the job's return value (Succeeded state).
/// </summary>
[ApiController]
[Route($"{RouteConstant.CONTROLLER}")]
public sealed class JobController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IStorageRepository _storage;
    private readonly JobStorage _jobStorage;

    public JobController(IStorageRepository storage, JobStorage jobStorage)
    {
        _storage = storage;
        _jobStorage = jobStorage;
    }

    /// <summary>Get job status by polling Hangfire state.</summary>
    [HttpGet("{jobId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public IActionResult GetStatus(string jobId)
    {
        var monitor = _jobStorage.GetMonitoringApi();
        var jobDetails = monitor.JobDetails(jobId);

        if (jobDetails is null)
        {
            return Ok(new { jobId, status = "NotFound" });
        }

        var state = jobDetails.History
            .Select(h => h.StateName)
            .LastOrDefault() ?? "Unknown";

        return state switch
        {
            "Succeeded" => Ok(new
            {
                jobId,
                status = "Completed",
                downloadUrl = $"/api/v1/jobs/{jobId}/download"
            }),
            "Failed" => Ok(new
            {
                jobId,
                status = "Failed",
                error = jobDetails.History
                    .Select(h => h.Data?.TryGetValue("ErrorMessage", out var msg) == true ? msg : null)
                    .LastOrDefault(msg => msg != null) ?? "Unknown error"
            }),
            _ => Ok(new { jobId, status = state })
        };
    }

    /// <summary>Download completed export file (streamed from storage via AssetModel).</summary>
    [HttpGet("{jobId}/download")]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(404)]
    public async Task<IResult> Download(string jobId, CancellationToken cancellationToken)
    {
        var assetId = await ResolveAssetIdAsync(jobId);
        if (assetId is null)
        {
            return Results.NotFound(new { error = "Export result not found or job not yet completed" });
        }

        var result = await _storage.GetFileStreamAsync(assetId.Value, cancellationToken);
        if (result is null)
        {
            return Results.NotFound(new { error = "Export file not found in storage" });
        }

        return Results.Stream(result.Stream, result.ContentType, result.FileName);
    }

    /// <summary>
    ///     Extract the export asset ID from the job's Succeeded state return value.
    ///     ExportJob returns JobResult with DownloadUrl = asset.Id.ToString().
    /// </summary>
    private async Task<Guid?> ResolveAssetIdAsync(string jobId)
    {
        var monitor = _jobStorage.GetMonitoringApi();
        var jobDetails = monitor.JobDetails(jobId);

        if (jobDetails?.History is null) return null;

        var succeeded = jobDetails.History.LastOrDefault(h => h.StateName == "Succeeded");
        if (succeeded?.Data is null) return null;

        // Hangfire stores the job return value in SucceededState.Data["ReturnValue"]
        if (!succeeded.Data.TryGetValue("ReturnValue", out var returnValueJson)
            || string.IsNullOrEmpty(returnValueJson))
        {
            return null;
        }

        try
        {
            var result = JsonSerializer.Deserialize<JobResult>(returnValueJson, JsonOpts);
            if (result?.DownloadUrl is null) return null;

            return Guid.Parse(result.DownloadUrl);
        }
        catch
        {
            return null;
        }
    }
}
