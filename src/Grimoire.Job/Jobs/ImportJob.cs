namespace Grimoire.Job.Jobs;

using System.Text.Json;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Import;
using Grimoire.Application.Publish;
using Grimoire.Application.Publish.Dto;
using Grimoire.Application.Publish.Import;
using Grimoire.Domain.Common.Repository;
using Grimoire.Infrastructure.Configuration;
using Hangfire;
using Hangfire.Server;

public sealed partial class ImportJob(IServiceScopeFactory scopeFactory, IJobProgressTracker progressTracker) : JobBase(scopeFactory, progressTracker) {
	private string? _seriesDtoJson;
	private string? _volumesJson;
	private string _fileKey = "";

	[DisableConcurrentExecution(timeoutInSeconds: 300)]
	[AutomaticRetry(Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
	public async Task<JobResult?> ExecuteAsync(
		PerformContext? context,
		string? seriesDtoJson,
		string? volumesJson,
		string fileKey,
		CancellationToken cancellationToken) {
		_seriesDtoJson = seriesDtoJson;
		_volumesJson = volumesJson;
		_fileKey = fileKey;
		return await ExecuteInternalAsync(context, nameof(ImportJob), cancellationToken);
	}

	protected override async Task<JobResult?> ExecuteCoreAsync(JobContext ctx, CancellationToken cancellationToken) {
		var jobId = ctx.JobId;
		var services = ctx.Services;

		CreateSeriesRequestDto? seriesDto = null;
		if (!string.IsNullOrEmpty(_seriesDtoJson)) {
			seriesDto = JsonSerializer.Deserialize<CreateSeriesRequestDto>(_seriesDtoJson, JsonConfiguration.JsonOptions);
		}

		List<ImportVolumeDto>? volumesOverride = null;
		if (!string.IsNullOrEmpty(_volumesJson)) {
			volumesOverride = JsonSerializer.Deserialize<List<ImportVolumeDto>>(_volumesJson, JsonConfiguration.JsonOptions);
		}

		var strategyFactory = services.GetRequiredService<ImportStrategyFactory>();
		var strategy = strategyFactory.GetStrategy(_fileKey);

		var storage = services.GetRequiredService<IStorageRepository>();
		await using var sourceStream = await storage.GetFileByPathAsync(_fileKey, cancellationToken)
			?? throw new InvalidOperationException($"Source file not found: {_fileKey}");

		var pipeline = services.GetRequiredService<IImportPipeline>();

		var pipelineContext = new ImportPipelineContext(strategy, seriesDto, volumesOverride, sourceStream, jobId);
		pipelineContext.OnProgress = progress => ctx.Progress.Report(progress, pipelineContext.CurrentStage);

		await pipeline.ExecuteAsync(pipelineContext, cancellationToken);
		if (pipelineContext.Result is { Success: true }) {
			await storage.DeleteFileByPathAsync(_fileKey, cancellationToken);
		}

		if (pipelineContext.Result is not null) {
			LogImportJobCompleted(ctx.Logger, jobId, pipelineContext.Series?.Id,
				pipelineContext.ResolvedVolumes.Count(v => v.WasCreated),
				pipelineContext.ResolvedVolumes.Count(v => !v.WasCreated),
				pipelineContext.ChaptersCreated, pipelineContext.ChaptersUpdated);
		}

		return pipelineContext.Result;
	}

	[LoggerMessage(LogLevel.Information,
		"ImportJob completed — JobId={JobId}, SeriesId={SeriesId}, " +
		"VolumesCreated={VolumesCreated}, VolumesUpdated={VolumesUpdated}, " +
		"ChaptersCreated={ChaptersCreated}, ChaptersUpdated={ChaptersUpdated}")]
	private static partial void LogImportJobCompleted(ILogger logger, string? jobId, Guid? seriesId,
		int volumesCreated, int volumesUpdated, int chaptersCreated, int chaptersUpdated);
}
