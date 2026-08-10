namespace Grimoire.Application.Ingestion.Execution;

using System.Text.Json;
using System.Text.Json.Serialization;
using Analysis;
using Contract;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Entity.Ingestion;
using Dto.Book;
using Dto.Book.Metadata;
using Dto.Book.Segment;
using Import;
using Mapper;
using Reconciliation;
using Service.Contract;

public sealed class ImportExecutionService(
	IImportRunRepository runRepository,
	IImportSourceRepository sourceRepository,
	IImportBindingRepository bindingRepository,
	ISeriesRepository seriesRepository,
	IUnitOfWork unitOfWork,
	IBookTreeService bookTreeService,
	IChapterService chapterService,
	IStorageRepository storageRepository,
	IXhtmlSegmentParser xhtmlParser,
	IBookMapper mapper,
	IImportAnalysisService analysisService,
	ISeriesRevisionService revisionService) : IImportExecutionService {
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
	};

	public async Task<ImportRunResponseDto> SaveDecisions(
		Guid importRunId,
		UpdateImportDecisionsDto request,
		CancellationToken cancellationToken = default) {
		var run = await runRepository.FindOneTracked(importRunId, cancellationToken)
			?? throw new KeyNotFoundException($"Import run '{importRunId}' does not exist.");
		var plan = DeserializePlan(run);
		var knownIds = plan.Operations.Select(static operation => operation.Id).ToHashSet(StringComparer.Ordinal);
		if (request.Decisions.Select(static decision => decision.OperationId).Distinct(StringComparer.Ordinal).Count() != request.Decisions.Count) {
			throw new ArgumentException("An operation can have only one decision.");
		}
		if (request.Decisions.Any(decision => !knownIds.Contains(decision.OperationId))) {
			throw new ArgumentException("A decision references an operation that is not in this plan.");
		}

		run.DecisionsJson = JsonSerializer.Serialize(request.Decisions, JsonOptions);
		var decided = request.Decisions.Select(static decision => decision.OperationId).ToHashSet(StringComparer.Ordinal);
		run.Status = plan.Operations.Where(static operation => operation.Disposition == PlanDisposition.Review)
			.All(operation => decided.Contains(operation.Id))
			? ImportRunStatus.ReadyToCommit
			: ImportRunStatus.AwaitingDecision;
		await runRepository.Update(run, cancellationToken);
		return (await analysisService.Find(importRunId, cancellationToken))!;
	}

	public async Task<ImportRunResponseDto> Commit(Guid importRunId, bool safeOnly, CancellationToken cancellationToken = default) {
		using var revisionBatch = revisionService.Suppress();
		await unitOfWork.BeginTransactionAsync(cancellationToken);
		try {
			var run = await runRepository.FindOneTracked(importRunId, cancellationToken)
				?? throw new KeyNotFoundException($"Import run '{importRunId}' does not exist.");
			if (run.Status == ImportRunStatus.Committed) {
				await unitOfWork.CommitTransactionAsync(cancellationToken);
				return (await analysisService.Find(importRunId, cancellationToken))!;
			}
			if (run.TargetSeriesId is null || run.BaseSeriesRevision is null) {
				throw new InvalidOperationException("The import has no resolved target series.");
			}

			var series = await seriesRepository.FindOneTracked(run.TargetSeriesId.Value, cancellationToken)
				?? throw new KeyNotFoundException($"Target series '{run.TargetSeriesId}' does not exist.");
			if (series.Revision != run.BaseSeriesRevision.Value) {
				throw new InvalidOperationException(
					$"StalePlan: series revision is {series.Revision}, plan expected {run.BaseSeriesRevision.Value}.");
			}

			var plan = DeserializePlan(run);
			var decisions = DeserializeDecisions(run);
			var reviewOperations = plan.Operations.Where(static operation => operation.Disposition == PlanDisposition.Review).ToList();
			if (!safeOnly && reviewOperations.Any(operation => !decisions.ContainsKey(operation.Id))) {
				throw new InvalidOperationException("Every review operation requires an approve or reject decision before commit.");
			}
			if (plan.Operations.Any(static operation => operation.Disposition == PlanDisposition.Reject)) {
				throw new InvalidOperationException("A plan containing rejected/invalid operations cannot be committed.");
			}

			var selected = plan.Operations.Where(operation =>
				operation.Disposition == PlanDisposition.Automatic ||
				(!safeOnly && decisions.GetValueOrDefault(operation.Id)?.Choice == ImportDecisionChoice.Approve)).ToList();
			var createdTargets = new Dictionary<string, string>(StringComparer.Ordinal);
			var treeMutated = false;
			foreach (var operation in selected) {
				var targetId = operation.TargetNodeId;
				var contentCommitted = false;
				if (operation.Type == "CreateNode") {
					targetId = await ExecuteCreate(operation, createdTargets, run.TargetSeriesId.Value, cancellationToken);
					createdTargets[operation.Id] = targetId;
					treeMutated = true;
					contentCommitted = operation.SourceNode?.Kind == ReconciliationNodeKind.Content;
				}
				else if (operation.Type is "ReplaceChapterContent" or "AppendChapterContent") {
					if (targetId is null) throw new InvalidOperationException($"Operation '{operation.Id}' has no chapter target.");
					await ExecuteChapterContent(operation, append: operation.Type == "AppendChapterContent", run.TargetSeriesId.Value, cancellationToken);
					treeMutated = true;
					contentCommitted = true;
				}
				else if (operation.Type is not ("NoOp" or "RecordEvidence")) {
					throw new InvalidOperationException($"Plan operation '{operation.Type}' is not executable yet.");
				}

				if (!string.IsNullOrWhiteSpace(targetId) && !string.IsNullOrWhiteSpace(operation.ExternalNodeKey)) {
					await UpsertBinding(run.ImportSourceId, operation, targetId, contentCommitted, cancellationToken);
				}
			}

			var source = await sourceRepository.FindOneTracked(run.ImportSourceId, cancellationToken)
				?? throw new KeyNotFoundException($"Import source '{run.ImportSourceId}' does not exist.");
			source.TargetSeriesId = run.TargetSeriesId;
			await sourceRepository.Update(source, cancellationToken);

			if (treeMutated) {
				series.AdvanceRevision(run.BaseSeriesRevision.Value);
				await seriesRepository.Update(series, cancellationToken);
			}

			var remaining = safeOnly
				? plan.Operations.Where(static operation => operation.Disposition == PlanDisposition.Review).Select(operation => RewriteParent(operation, createdTargets)).ToList()
				: [];
			if (remaining.Count == 0) {
				run.Status = ImportRunStatus.Committed;
				run.CommittedAt = DateTimeOffset.UtcNow;
			}
			else {
				run.Status = ImportRunStatus.AwaitingDecision;
				run.BaseSeriesRevision = series.Revision;
				run.PlanJson = JsonSerializer.Serialize(plan with {
					BaseSeriesRevision = series.Revision,
					Operations = remaining,
					Summary = Summarize(remaining)
				}, JsonOptions);
			}
			await runRepository.Update(run, cancellationToken);
			await unitOfWork.CommitTransactionAsync(cancellationToken);
			return (await analysisService.Find(importRunId, cancellationToken))!;
		}
		catch {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}
	}

	private async Task ExecuteChapterContent(
		PlanOperationDto operation,
		bool append,
		Guid seriesId,
		CancellationToken cancellationToken) {
		var chapterId = PrefixedId.ToGuid(operation.TargetNodeId!, EntityPrefix.Chapter);
		var current = await chapterService.GetWithContentAsync(chapterId, cancellationToken)
			?? throw new KeyNotFoundException($"Chapter '{operation.TargetNodeId}' does not exist.");
		var source = operation.SourceNode ?? throw new InvalidOperationException($"Operation '{operation.Id}' has no source content.");
		var incoming = await MaterializeContent(source.Content, cancellationToken);
		var existingSegments = append ? current.Segments.Select(mapper.ToSegmentDto).ToList() : [];
		var existingFootnotes = append
			? current.Segments.OfType<Domain.Entity.Book.Segment.FootnoteSegmentModel>()
				.Select(segment => new ImportFootnoteDto {
					InitialId = segment.Id.ToString(),
					Segments = [.. segment.Segments.Select(mapper.ToTextSegmentDto)]
				}).ToList()
			: [];
		var segments = incoming.RawMarkdown is null
			? existingSegments.Concat(incoming.Segments ?? []).ToList()
			: incoming.Segments;
		var rawMarkdown = append && incoming.RawMarkdown is not null
			? throw new InvalidOperationException("Appending raw Markdown requires producer-side segmentation.")
			: incoming.RawMarkdown;
		var dto = new CreateChapterRequestDto(
			PrefixedId.ToString(EntityPrefix.Volume, current.Chapter.Path.GetVolumeId()),
			current.Chapter.Order,
			current.Chapter.Title,
			segments,
			[.. existingFootnotes, .. incoming.Footnotes ?? []],
			rawMarkdown);
		await chapterService.UpsertAsync(current.Chapter.Path.GetVolumeId(), dto, current.Chapter, seriesId, cancellationToken);
	}

	private async Task<string> ExecuteCreate(
		PlanOperationDto operation,
		IReadOnlyDictionary<string, string> createdTargets,
		Guid seriesId,
		CancellationToken cancellationToken) {
		var source = operation.SourceNode ?? throw new InvalidOperationException($"Operation '{operation.Id}' has no source payload.");
		var parentId = operation.ParentTargetId;
		if (parentId is null && operation.ParentOperationId is not null) {
			createdTargets.TryGetValue(operation.ParentOperationId, out parentId);
		}
		if (parentId is null) throw new InvalidOperationException($"Operation '{operation.Id}' has no materialized parent.");

		var order = await ResolveOrder(parentId, source.OrderHint, operation.PreviousTargetId, operation.NextTargetId, seriesId, cancellationToken);
		if (source.Kind == ReconciliationNodeKind.Container && parentId.StartsWith("ser_", StringComparison.Ordinal)) {
			var volume = await bookTreeService.CreateVolume(new CreateVolumeRequestDto(
				parentId, order, source.Title, ReadVolumeMetadata(source)), cancellationToken);
			return PrefixedId.ToString(EntityPrefix.Volume, volume.Id);
		}
		if (source.Kind == ReconciliationNodeKind.Content && parentId.StartsWith("vol_", StringComparison.Ordinal)) {
			var content = await MaterializeContent(source.Content, cancellationToken);
			var chapter = await chapterService.Create(new CreateChapterRequestDto(parentId, order, source.Title,
				content.Segments, content.Footnotes, content.RawMarkdown), cancellationToken);
			return PrefixedId.ToString(EntityPrefix.Chapter, chapter.Id);
		}

		throw new InvalidOperationException(
			$"Cannot materialize {source.Kind} node '{source.ExternalKey}' under '{parentId}' in the Series/Volume/Chapter tree.");
	}

	private static VolumeMetadataDto? ReadVolumeMetadata(SourceNodeDto source) {
		if (source.Metadata is null) return null;
		var cover = source.Metadata.TryGetValue("coverImage", out var coverElement) && coverElement.ValueKind == JsonValueKind.String
			? coverElement.GetString()
			: null;
		var isbn = source.Metadata.TryGetValue("isbn", out var isbnElement) && isbnElement.ValueKind == JsonValueKind.String
			? isbnElement.GetString()
			: null;
		DateTime? publicationDate = null;
		if (source.Metadata.TryGetValue("publicationDate", out var dateElement) && dateElement.ValueKind == JsonValueKind.String &&
			DateTime.TryParse(dateElement.GetString(), out var parsed)) publicationDate = parsed;
		return cover is null && isbn is null && publicationDate is null
			? null
			: new VolumeMetadataDto { CoverImage = cover, Isbn = isbn, PublicationDate = publicationDate };
	}

	private async Task<(List<SegmentDto>? Segments, List<ImportFootnoteDto>? Footnotes, string? RawMarkdown)> MaterializeContent(
		SourceContentDto? content,
		CancellationToken cancellationToken) {
		if (content is null) return ([], [], null);
		var inline = content.Inline;
		if (inline is null && content.StagingObjectKey is not null) {
			await using var stream = await storageRepository.GetFileByPathAsync(content.StagingObjectKey, cancellationToken)
				?? throw new InvalidOperationException($"Staged content '{content.StagingObjectKey}' does not exist.");
			using var reader = new StreamReader(stream);
			inline = await reader.ReadToEndAsync(cancellationToken);
		}
		inline ??= string.Empty;

		return content.Format switch {
			SourceContentFormat.Markdown => (null, content.Footnotes?.ToList(), inline),
			SourceContentFormat.Segments => (
				JsonSerializer.Deserialize<List<SegmentDto>>(inline, JsonOptions) ?? [], content.Footnotes?.ToList() ?? [], null),
			SourceContentFormat.Html => MergeFootnotes(ParseHtml(inline), content.Footnotes),
			_ => throw new InvalidOperationException($"Unsupported source content format '{content.Format}'.")
		};
	}

	private static (List<SegmentDto>, List<ImportFootnoteDto>, string?) MergeFootnotes(
		(List<SegmentDto> Segments, List<ImportFootnoteDto> Footnotes, string? RawMarkdown) parsed,
		IReadOnlyList<ImportFootnoteDto>? supplied) => supplied is null
			? parsed
			: (parsed.Segments, [.. parsed.Footnotes, .. supplied], parsed.RawMarkdown);

	private (List<SegmentDto>, List<ImportFootnoteDto>, string?) ParseHtml(string html) {
		var parsed = xhtmlParser.Parse(html, new Dictionary<string, byte[]>());
		return ([.. parsed.Segments.Select(mapper.ToSegmentDto)], parsed.Footnotes, null);
	}

	private async Task<double> ResolveOrder(
		string parentId,
		double? hint,
		string? previousTargetId,
		string? nextTargetId,
		Guid seriesId,
		CancellationToken cancellationToken) {
		if (parentId.StartsWith("ser_", StringComparison.Ordinal)) {
			var siblings = (await bookTreeService.FindVolumes(seriesId, cancellationToken)).ToList();
			return FractionalOrderAllocator.Allocate(siblings.Select(volume => (PrefixedId.ToString(EntityPrefix.Volume, volume.Id), volume.Order)), hint, previousTargetId, nextTargetId);
		}
		if (parentId.StartsWith("vol_", StringComparison.Ordinal)) {
			var volumeId = PrefixedId.ToGuid(parentId, EntityPrefix.Volume);
			var siblings = (await bookTreeService.FindChapters(volumeId, cancellationToken)).ToList();
			return FractionalOrderAllocator.Allocate(siblings.Select(chapter => (PrefixedId.ToString(EntityPrefix.Chapter, chapter.Id), chapter.Order)), hint, previousTargetId, nextTargetId);
		}
		throw new InvalidOperationException($"Unsupported parent '{parentId}'.");
	}

	private async Task UpsertBinding(
		Guid sourceId,
		PlanOperationDto operation,
		string targetId,
		bool contentCommitted,
		CancellationToken cancellationToken) {
		var binding = await bindingRepository.FindBySourceNode(sourceId, operation.ExternalNodeKey, cancellationToken);
		var (targetNodeId, nodeType) = ParseTarget(targetId);
		var isNew = binding is null;
		var baseFingerprint = (contentCommitted || isNew) && nodeType == BookNodeType.Chapter
			? await FingerprintChapter(targetNodeId, cancellationToken)
			: null;
		if (binding is null) {
			await bindingRepository.Create(new ImportBindingModel {
				ImportSourceId = sourceId,
				ExternalNodeKey = operation.ExternalNodeKey,
				TargetNodeId = targetNodeId,
				TargetNodeType = nodeType,
				Role = NormalizeRole(operation.SourceNode?.RoleHint),
				LastImportedHash = operation.SourceNode?.Content?.ContentHash,
				BaseSnapshotKey = baseFingerprint,
				LastSeenAt = DateTimeOffset.UtcNow
			}, cancellationToken);
			return;
		}

		binding.TargetNodeId = targetNodeId;
		binding.TargetNodeType = nodeType;
		binding.Role = NormalizeRole(operation.SourceNode?.RoleHint ?? binding.Role);
		binding.LastImportedHash = operation.SourceNode?.Content?.ContentHash ?? binding.LastImportedHash;
		if (baseFingerprint is not null) binding.BaseSnapshotKey = baseFingerprint;
		binding.LastSeenAt = DateTimeOffset.UtcNow;
		await bindingRepository.Update(binding, cancellationToken);
	}

	private async Task<string> FingerprintChapter(Guid chapterId, CancellationToken cancellationToken) {
		var chapter = await chapterService.GetWithContentAsync(chapterId, cancellationToken)
			?? throw new KeyNotFoundException($"Chapter '{chapterId}' does not exist.");
		return BookContentFingerprint.Compute(chapter.Segments);
	}

	private static (Guid Id, BookNodeType Type) ParseTarget(string targetId) {
		if (targetId.StartsWith("ser_", StringComparison.Ordinal)) return (PrefixedId.ToGuid(targetId, EntityPrefix.Series), BookNodeType.Series);
		if (targetId.StartsWith("vol_", StringComparison.Ordinal)) return (PrefixedId.ToGuid(targetId, EntityPrefix.Volume), BookNodeType.Volume);
		if (targetId.StartsWith("chp_", StringComparison.Ordinal)) return (PrefixedId.ToGuid(targetId, EntityPrefix.Chapter), BookNodeType.Chapter);
		throw new InvalidOperationException($"Unsupported target ID '{targetId}'.");
	}

	private static string NormalizeRole(string? role) => role?.ToLowerInvariant() switch {
		"supplement" => nameof(CompositionRole.Supplement),
		"reference" => nameof(CompositionRole.Reference),
		_ => nameof(CompositionRole.Primary)
	};

	private static PlanOperationDto RewriteParent(PlanOperationDto operation, IReadOnlyDictionary<string, string> createdTargets) =>
		operation.ParentOperationId is not null && createdTargets.TryGetValue(operation.ParentOperationId, out var target)
			? operation with { ParentTargetId = target, ParentOperationId = null }
			: operation;

	private static PlanSummaryDto Summarize(IReadOnlyList<PlanOperationDto> operations) => new(
		operations.Count(static operation => operation.Disposition == PlanDisposition.Automatic && operation.Type != "NoOp"),
		operations.Count(static operation => operation.Disposition == PlanDisposition.Review),
		operations.Count(static operation => operation.Disposition == PlanDisposition.Reject),
		operations.Count(static operation => operation.Type == "NoOp"));

	private static ReconciliationPlanDto DeserializePlan(ImportRunModel run) =>
		run.PlanJson is null
			? throw new InvalidOperationException("The import run has no reconciliation plan.")
			: JsonSerializer.Deserialize<ReconciliationPlanDto>(run.PlanJson, JsonOptions)
				?? throw new InvalidOperationException("The stored reconciliation plan is invalid.");

	private static IReadOnlyDictionary<string, ImportOperationDecisionDto> DeserializeDecisions(ImportRunModel run) =>
		(run.DecisionsJson is null
			? []
			: JsonSerializer.Deserialize<List<ImportOperationDecisionDto>>(run.DecisionsJson, JsonOptions) ?? [])
		.ToDictionary(static decision => decision.OperationId, StringComparer.Ordinal);
}
