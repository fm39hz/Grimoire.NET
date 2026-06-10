namespace Grimoire.Application.Publish;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Publish.Dto;

public interface IPublishService
{
    Task<string> EnqueueExportAsync(Guid seriesId, BinderyRequestDto request, CancellationToken cancellationToken = default);
    Task<string> EnqueueImportAsync(CreateSeriesRequestDto seriesDto, List<ImportVolumeDto>? volumesOverride, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<PublishJobStatusDto?> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default);
    Task<PublishDownloadResultDto?> GetDownloadStreamAsync(string jobId, CancellationToken cancellationToken = default);
}
