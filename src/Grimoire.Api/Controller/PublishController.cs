namespace Grimoire.Api.Controller;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Constant;
using Application.Dto.Book;
using Application.Publish;
using Domain.Common;
using Grimoire.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class PublishController(IPublishService publishService) : ControllerBase
{
    [HttpPost("series/{seriesId}/export")]
    [ProducesResponseType(202)]
    [ProducesResponseType(400)]
    public async Task<IResult> ExportSeries(
        [FromRoute] string seriesId,
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
            return Results.BadRequest("Invalid seriesId format");
        }

        var jobId = await publishService.EnqueueExportAsync(guid, request, cancellationToken);
        var statusUrl = $"/api/{RouteConstant.VERSION}/publishes/jobs/{jobId}";

        return Results.Accepted(statusUrl, new
        {
            jobId,
            status = "Queued",
            statusUrl
        });
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(202)]
    [ProducesResponseType(400)]
    public async Task<IResult> ImportBook(
        [FromForm] string? series,
        [FromForm] string? volumes,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest("EPUB file is required");

        if (!file.FileName.EndsWith(".epub", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest("File must be an EPUB file (.epub)");

        CreateSeriesRequestDto? seriesDto = null;
        if (!string.IsNullOrEmpty(series))
        {
            try
            {
                seriesDto = JsonSerializer.Deserialize<CreateSeriesRequestDto>(series, JsonConfiguration.JsonOptions);
            }
            catch (JsonException)
            {
                return Results.BadRequest("Invalid series metadata JSON");
            }

            if (seriesDto is null)
                return Results.BadRequest("Invalid series metadata JSON");
        }

        List<ImportVolumeDto>? volumesOverride = null;
        if (!string.IsNullOrEmpty(volumes))
        {
            try
            {
                volumesOverride = JsonSerializer.Deserialize<List<ImportVolumeDto>>(volumes, JsonConfiguration.JsonOptions);
            }
            catch (JsonException)
            {
                return Results.BadRequest("Invalid volumes metadata JSON");
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
        var statusUrl = $"/api/{RouteConstant.VERSION}/publishes/jobs/{jobId}";

        return Results.Accepted(statusUrl, new
        {
            jobId,
            status = "Queued",
            statusUrl
        });
    }

    [HttpGet("jobs/{jobId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IResult> GetStatus(string jobId, CancellationToken cancellationToken)
    {
        var status = await publishService.GetJobStatusAsync(jobId, cancellationToken);
        if (status is null)
        {
            return Results.NotFound(new { jobId, status = "NotFound" });
        }
        return Results.Ok(status);
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
