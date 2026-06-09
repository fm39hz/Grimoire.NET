namespace Grimoire.Application.Publish.Dto;

using System.IO;

public sealed record PublishDownloadResultDto(
    Stream Stream,
    string ContentType,
    string FileName
);
