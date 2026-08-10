namespace Grimoire.Application.Ingestion.Analysis;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Contract;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Ingestion;
using Service.Contract;
using Research;
using Microsoft.Extensions.Logging;

public sealed class ImportAnalysisService(
	IImportRunRepository runRepository,
	IImportSourceRepository sourceRepository,
	IImportBindingRepository bindingRepository,
	ISeriesRepository seriesRepository,
	IBookTreeService bookTreeService,
	ISeriesTargetResolver targetResolver,
	SourcePackagePlanBuilder planBuilder,
	IResearchCoordinator researchCoordinator,
	ILogger<ImportAnalysisService> logger) : IImportAnalysisService {
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
	};

	public async Task<ImportRunResponseDto> Analyze(SourcePackageDto package, CancellationToken cancellationToken = default) {
		var packageJson = JsonSerializer.Serialize(package, JsonOptions);
		var packageHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(packageJson)));
		var existingRun = await runRepository.FindByIdempotencyKey(package.Producer.Id, package.IdempotencyKey, cancellationToken);
		if (existingRun is not null) {
			if (existingRun.PackageHash != packageHash) {
				throw new InvalidOperationException("The idempotency key is already associated with a different source package.");
			}
			return ToResponse(existingRun);
		}

		var importSource = await sourceRepository.FindByIdentity(package.Producer.Id, package.Source.ExternalKey, cancellationToken);
		var resolution = await targetResolver.Resolve(package.Producer.Id, package.TargetHint, cancellationToken);
		var hasExplicitTarget = !string.IsNullOrWhiteSpace(package.TargetHint?.SeriesId);
		var targetSeriesId = hasExplicitTarget ? resolution.SeriesId : importSource?.TargetSeriesId ?? resolution.SeriesId;
		var targetSeries = targetSeriesId is null
			? null
			: await seriesRepository.FindOne(targetSeriesId.Value, cancellationToken)
				?? throw new ArgumentException($"Target series '{package.TargetHint?.SeriesId}' does not exist.");

		if (importSource is null) {
			importSource = await sourceRepository.Create(new ImportSourceModel {
				ProducerId = package.Producer.Id,
				ExternalKey = package.Source.ExternalKey,
				Provider = package.Source.Provider,
				Uri = package.Source.Uri,
				TargetSeriesId = targetSeriesId,
				LastPackageHash = packageHash,
				LastSeenAt = package.Source.ObservedAt ?? DateTimeOffset.UtcNow,
				MetadataJson = JsonSerializer.Serialize(package.Source.Metadata ?? new Dictionary<string, JsonElement>(), JsonOptions)
			}, cancellationToken);
		}
		else {
			importSource.Provider = package.Source.Provider;
			importSource.Uri = package.Source.Uri;
			importSource.TargetSeriesId ??= targetSeriesId;
			importSource.LastPackageHash = packageHash;
			importSource.LastSeenAt = package.Source.ObservedAt ?? DateTimeOffset.UtcNow;
			await sourceRepository.Update(importSource, cancellationToken);
		}

		var run = new ImportRunModel {
			ProducerId = package.Producer.Id,
			IdempotencyKey = package.IdempotencyKey,
			TargetSeriesId = targetSeriesId,
			ImportSourceId = importSource.Id,
			Semantics = package.Semantics,
			PackageJson = packageJson,
			PackageHash = packageHash,
			BaseSeriesRevision = targetSeries?.Revision
		};
		await runRepository.Create(run, cancellationToken);

		var tree = targetSeriesId is null
			? null
			: await bookTreeService.GetTree(targetSeriesId.Value, includeContent: true, cancellationToken: cancellationToken);
		var bindings = await bindingRepository.FindBySource(importSource.Id, cancellationToken);
		var seriesSources = targetSeriesId is null
			? []
			: await sourceRepository.FindByTargetSeries(targetSeriesId.Value, cancellationToken);
		var allBindings = await bindingRepository.FindBySources(seriesSources.Select(static source => source.Id).ToArray(), cancellationToken);
		var targetId = targetSeriesId is null ? null : PrefixedId.ToString(EntityPrefix.Series, targetSeriesId.Value);
		var plan = planBuilder.Build(run.Id, package, targetId, targetSeries?.Revision, tree, bindings, importSource.Id, allBindings);
		if (targetSeriesId is not null) {
			try {
				await researchCoordinator.ObserveSourcePackage(targetSeriesId.Value, package, cancellationToken);
			}
			catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested) {
				logger.LogWarning(exception, "Could not persist source research evidence for import {ImportRunId}.", run.Id);
			}
		}

		run.PlanJson = JsonSerializer.Serialize(plan, JsonOptions);
		run.AnalysisJson = JsonSerializer.Serialize(new {
			packageHash,
			sourceId = importSource.Id,
			targetResolution = resolution,
			operationCount = plan.Operations.Count,
			issueCount = plan.Issues.Count
		}, JsonOptions);
		run.AnalyzedAt = DateTimeOffset.UtcNow;
		run.Status = plan.Issues.Count > 0 || plan.Operations.Any(static operation => operation.Disposition == Reconciliation.PlanDisposition.Review)
			? ImportRunStatus.AwaitingDecision
			: ImportRunStatus.Analyzed;
		await runRepository.Update(run, cancellationToken);
		return ToResponse(run);
	}

	public async Task<ImportRunResponseDto?> Find(Guid importRunId, CancellationToken cancellationToken = default) {
		var run = await runRepository.FindOne(importRunId, cancellationToken);
		return run is null ? null : ToResponse(run);
	}

	public async Task RecordLegacyOutcome(Guid importRunId, string outcomeJson, CancellationToken cancellationToken = default) {
		var run = await runRepository.FindOneTracked(importRunId, cancellationToken)
			?? throw new KeyNotFoundException($"Import run '{importRunId}' does not exist.");
		run.LegacyOutcomeJson = outcomeJson;
		await runRepository.Update(run, cancellationToken);
	}

	public async Task<IReadOnlyList<ImportRunResponseDto>> List(int limit = 50, CancellationToken cancellationToken = default) =>
		[.. (await runRepository.FindRecent(limit, cancellationToken)).Select(ToResponse)];

	private static ImportRunResponseDto ToResponse(ImportRunModel run) => new(
		$"imp_{run.Id}",
		run.Status.ToString(),
		run.ProducerId,
		run.IdempotencyKey,
		run.TargetSeriesId is null ? null : PrefixedId.ToString(EntityPrefix.Series, run.TargetSeriesId.Value),
		run.BaseSeriesRevision,
		run.StartedAt,
		run.AnalyzedAt,
		run.PlanJson is null ? null : JsonSerializer.Deserialize<ReconciliationPlanDto>(run.PlanJson, JsonOptions));

}
