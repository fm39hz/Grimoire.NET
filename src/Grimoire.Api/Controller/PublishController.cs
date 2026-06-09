namespace Grimoire.Api.Controller;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Api.Constant;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Publish;
using Grimoire.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class PublishController(IPublishService publishService) : ControllerBase
{
    [HttpPost("export")]
    [ProducesResponseType(202)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ExportSeries(
        [FromQuery] string seriesId,
        [FromBody] BinderyRequestDto request,
        CancellationToken cancellationToken)
    {
        Guid guid;
        try
        {
            guid = PrefixedId.ToGuid(seriesId, EntityPrefix.Series);
        }
        catch (Exception)
        {
            return BadRequest("Invalid seriesId format");
        }

        var jobId = await publishService.EnqueueExportAsync(guid, request, cancellationToken);

        return Accepted($"/api/v1/publish/jobs/{jobId}", new
        {
            jobId,
            status = "Queued",
            statusUrl = $"/api/v1/publish/jobs/{jobId}"
        });
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(202)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ImportBook(
        [FromForm] string series,
        [FromForm] string? volumes,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest("EPUB file is required");

        if (!file.FileName.EndsWith(".epub", StringComparison.OrdinalIgnoreCase))
            return BadRequest("File must be an EPUB file (.epub)");

        CreateSeriesRequestDto? seriesDto;
        try
        {
            seriesDto = JsonSerializer.Deserialize<CreateSeriesRequestDto>(series);
        }
        catch (JsonException)
        {
            return BadRequest("Invalid series metadata JSON");
        }

        if (seriesDto is null)
            return BadRequest("Invalid series metadata JSON");

        List<ImportVolumeDto>? volumesOverride = null;
        if (!string.IsNullOrEmpty(volumes))
        {
            try
            {
                volumesOverride = JsonSerializer.Deserialize<List<ImportVolumeDto>>(volumes);
            }
            catch (JsonException)
            {
                return BadRequest("Invalid volumes metadata JSON");
            }
        }

        await using var fileStream = file.OpenReadStream();
        var jobId = await publishService.EnqueueImportAsync(
            seriesDto,
            volumesOverride,
            fileStream,
            file.FileName,
            file.ContentType,
            cancellationToken);

        return Accepted($"/api/v1/publish/jobs/{jobId}", new
        {
            jobId,
            status = "Queued",
            statusUrl = $"/api/v1/publish/jobs/{jobId}"
        });
    }

    [HttpGet("jobs/{jobId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetStatus(string jobId, CancellationToken cancellationToken)
    {
        var status = await publishService.GetJobStatusAsync(jobId, cancellationToken);
        if (status.Status == "NotFound")
        {
            return Ok(new { jobId, status = "NotFound" });
        }
        return Ok(status);
    }

    [HttpGet("jobs/{jobId}/download")]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(404)]
    public async Task<IResult> Download(string jobId, CancellationToken cancellationToken)
    {
        var result = await publishService.GetDownloadStreamAsync(jobId, cancellationToken);
        if (result is null)
        {
            return Results.NotFound(new { error = "Export result not found or job not yet completed" });
        }

        return Results.Stream(result.Stream, result.ContentType, result.FileName);
    }
}
