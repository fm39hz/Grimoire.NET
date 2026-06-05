using System.Threading;
using Grimoire.Api.Constant;
using Grimoire.Domain.Common.Repository;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace Grimoire.Api.Controller;

/// <summary>
///     Monitor background job status and download results.
///     The asset ID is read from the job's return value (Succeeded state).
/// </summary>
[ApiController]
[Route($"{RouteConstant.CONTROLLER}")]
public sealed class JobController : ControllerBase
{
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
            .FirstOrDefault() ?? "Unknown";

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
    ///     ExportJob returns asset.Id.ToString() — a GUID string.
    ///     Hangfire's monitoring API gives us the raw string value (already unwrapped from JSON).
    /// </summary>
    private async Task<Guid?> ResolveAssetIdAsync(string jobId)
    {
        var monitor = _jobStorage.GetMonitoringApi();
        var jobDetails = monitor.JobDetails(jobId);

        if (jobDetails?.History is null) return null;

        var succeeded = jobDetails.History.FirstOrDefault(h => h.StateName == "Succeeded");
        if (succeeded?.Data is null) return null;

        // Hangfire stores the job return value in SucceededState.Data["ReturnValue"]
        if (!succeeded.Data.TryGetValue("ReturnValue", out var returnValue)
            || string.IsNullOrEmpty(returnValue))
        {
            return null;
        }

        // returnValue is already a bare GUID string from the monitoring API
        if (Guid.TryParse(returnValue, out var assetId))
            return assetId;

        return null;
    }
}
