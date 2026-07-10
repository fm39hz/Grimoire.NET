namespace Grimoire.Application.Publish;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Publish.Dto;

public interface IPublishService {
	public Task<string> EnqueueExportAsync(Guid seriesId, BinderyRequestDto request, CancellationToken cancellationToken = default);
	public Task<string> EnqueueImportAsync(CreateSeriesRequestDto? seriesDto, List<ImportVolumeDto>? volumesOverride, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
	public Task<PublishJobStatusDto?> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default);
	public Task<PublishDownloadResultDto?> GetDownloadStreamAsync(string jobId, CancellationToken cancellationToken = default);
}
