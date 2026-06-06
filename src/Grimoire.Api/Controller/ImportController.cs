namespace Grimoire.Api.Controller;

using System.Text.Json;
using Application.Dto.Book;
using Application.Import;
using Constant;
using Domain.Common.Repository;
using Grimoire.Job.Jobs;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route($"{RouteConstant.CONTROLLER}")]
public sealed class ImportController(
    IBackgroundJobClient jobs,
    IStorageRepository storage) : ControllerBase {

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(202)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ImportEpub(
        [FromForm] string series,
        [FromForm] string? volumes,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest("EPUB file is required");

        if (!file.FileName.EndsWith(".epub", StringComparison.OrdinalIgnoreCase))
            return BadRequest("File must be an EPUB file (.epub)");

        var seriesDto = JsonSerializer.Deserialize<CreateSeriesRequestDto>(series);
        if (seriesDto is null)
            return BadRequest("Invalid series metadata JSON");

        await using var fileStream = file.OpenReadStream();
        var fileKey = await storage.UploadFileAsync(
            fileStream, file.ContentType, file.FileName,
            "staging/import",
            cancellationToken);

        var jobId = jobs.Enqueue<ImportEpubJob>(
            job => job.ExecuteAsync(
                null!,
                series,
                volumes,
                fileKey,
                CancellationToken.None));

        return Accepted($"/api/v1/jobs/{jobId}", new
        {
            jobId,
            status = "Queued",
            statusUrl = $"/api/v1/jobs/{jobId}"
        });
    }
}
