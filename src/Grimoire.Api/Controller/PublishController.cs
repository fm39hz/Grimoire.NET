namespace Grimoire.Api.Controller;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Constant;
using Application.Dto.Book;
using Application.Publish;
using Grimoire.Application.Publish.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Grimoire.Infrastructure.Configuration;
using Grimoire.Domain.Common;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class PublishController(
    IPublishService publishService,
    IJobProgressTracker progressTracker,
    IJobProgressSubscription progressSubscription) : ControllerBase
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

    [HttpGet("jobs/{jobId}/progress")]
    public async Task GetProgressStream(string jobId, CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // 1. Send initial status
        var initialStatus = await publishService.GetJobStatusAsync(jobId, cts.Token);
        if (initialStatus is not null)
        {
            await WriteSseEventAsync(initialStatus);
            if (initialStatus.Status == "Completed" || initialStatus.Status == "Failed")
            {
                return;
            }
        }

        // 2. Subscribe to in-memory tracker
        var channel = progressSubscription.Subscribe(jobId, cts.Token);

        // 3. Safety poller task (fallback)
        var safetyPollTask = Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), cts.Token);
                    var currentStatus = await publishService.GetJobStatusAsync(jobId, cts.Token);
                    if (currentStatus is not null)
                    {
                        if (currentStatus.Status == "Completed" || currentStatus.Status == "Failed")
                        {
                            progressTracker.CompleteJob(jobId, currentStatus.DownloadUrl);
                            if (currentStatus.Status == "Failed")
                            {
                                progressTracker.FailJob(jobId, currentStatus.Error ?? "Job failed");
                            }
                            break;
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
        }, cts.Token);

        // 4. Consume events and write to response stream
        try
        {
            await foreach (var update in channel.WithCancellation(cts.Token))
            {
                await WriteSseEventAsync(update);
                if (update.Status == "Completed" || update.Status == "Failed")
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected - normal flow
        }
        finally
        {
            await cts.CancelAsync();
            try
            {
                await safetyPollTask;
            }
            catch { }
        }
    }

    private async Task WriteSseEventAsync(PublishJobStatusDto status)
    {
        var json = JsonSerializer.Serialize(status, JsonConfiguration.JsonOptions);
        await Response.WriteAsync($"data: {json}\n\n");
        await Response.Body.FlushAsync();
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
