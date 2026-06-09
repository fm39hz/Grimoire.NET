namespace Grimoire.Application.Publish.Dto;

public sealed record PublishJobStatusDto(
    string JobId,
    string Status,
    string? DownloadUrl = null,
    string? Error = null
);
