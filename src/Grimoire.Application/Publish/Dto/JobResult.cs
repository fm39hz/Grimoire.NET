namespace Grimoire.Application.Publish.Dto;

/// <summary>Standard result returned by all jobs.</summary>
public sealed record JobResult {
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? DownloadUrl { get; init; }
    public string? FileName { get; init; }
    public string? ContentType { get; init; }

    public static JobResult Ok(string downloadUrl, string fileName, string contentType) => new() {
        Success = true,
        DownloadUrl = downloadUrl,
        FileName = fileName,
        ContentType = contentType
    };

    public static JobResult Fail(string error) => new() {
        Success = false,
        ErrorMessage = error
    };
}
