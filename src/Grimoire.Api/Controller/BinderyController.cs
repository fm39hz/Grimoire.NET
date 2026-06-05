namespace Grimoire.Api.Controller;

using Application.Dto.Book;
using Constant;
using Domain.Common;
using Grimoire.Job.Jobs;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

/// <summary>
///     Enqueue series export jobs. Processing happens in the Grimoire.Job worker.
/// </summary>
[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class BinderyController(IBackgroundJobClient jobs) : ControllerBase
{
    /// <summary>
    ///     Enqueue an export job. Returns 202 with job ID.
    ///     Poll GET /api/v1/jobs/{jobId} for status and download URL.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(202)]
    [ProducesResponseType(400)]
    public IActionResult ExportSeries(
        [FromQuery] string seriesId,
        [FromBody] BinderyRequestDto request)
    {
        var guid = PrefixedId.ToGuid(seriesId, EntityPrefix.Series);

        var jobId = jobs.Enqueue<ExportJob>(
            job => job.ExecuteAsync(
                null!,        // PerformContext — filled by Hangfire
                guid,
                request,
                CancellationToken.None));

        return Accepted($"/api/v1/jobs/{jobId}", new
        {
            jobId,
            status = "Queued",
            statusUrl = $"/api/v1/jobs/{jobId}"
        });
    }
}
