namespace Grimoire.Application.Publish.Export.Steps;

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Grimoire.Application.Dto.Book;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Publish.Dto;
using Grimoire.Application.Service.Contract;
using Grimoire.Application.Service.Strategy;
using Grimoire.Domain.Common;
using Grimoire.Domain.Common.Repository;

public sealed class ContentGenerationStep(
	IBinderyService bindery,
	IVolumeRepository volumeRepository,
	ISeriesRepository seriesRepository) : IExportPipelineStep {
	public int Order => 20;

	public async Task ExecuteAsync(ExportPipelineContext context, CancellationToken cancellationToken) {
		if (context.SkipExport) {
			return;
		}

		var isBatch = string.Equals(context.Request.Mode, "OnePerVolume", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(context.Request.Mode, "CustomGroups", StringComparison.OrdinalIgnoreCase);
		var exportResult = isBatch
			? await GenerateBatch(context, cancellationToken)
			: await bindery.ExportSeriesAsync(context.SeriesId, context.Request, cancellationToken);
		if (!exportResult.Success) {
			context.Result = JobResult.Fail(exportResult.ErrorMessage ?? "Export generation failed");
			return;
		}

		context.ExportResult = exportResult;
	}

	private async Task<ExportResult> GenerateBatch(ExportPipelineContext context, CancellationToken cancellationToken) {
		var series = await seriesRepository.FindOne(context.SeriesId, cancellationToken)
			?? throw new KeyNotFoundException($"Series '{context.SeriesId}' does not exist.");
		var volumes = (await volumeRepository.FindBySeriesId(context.SeriesId, cancellationToken))
			.OrderBy(static volume => volume.Order).ToList();
		var knownIds = volumes.Select(volume => PrefixedId.ToString(EntityPrefix.Volume, volume.Id)).ToHashSet(StringComparer.Ordinal);
		var groups = string.Equals(context.Request.Mode, "OnePerVolume", StringComparison.OrdinalIgnoreCase)
			? volumes.Select(volume => new ExportGroupDto(volume.Title, [PrefixedId.ToString(EntityPrefix.Volume, volume.Id)])).ToList()
			: context.Request.Groups ?? [];

		if (groups.Count == 0) return ExportResult.Fail("The batch contains no volume groups.");
		var unknown = groups.SelectMany(static group => group.TargetVolumeIds).Where(id => !knownIds.Contains(id)).Distinct().ToList();
		if (unknown.Count > 0) return ExportResult.Fail($"Unknown volume IDs: {string.Join(", ", unknown)}");

		var package = new MemoryStream();
		var artifacts = new List<PublishArtifactDto>(groups.Count);
		using (var zip = new ZipArchive(package, ZipArchiveMode.Create, leaveOpen: true)) {
			for (var index = 0; index < groups.Count; index++) {
				var group = groups[index];
				context.ReportSubProgress((double)index / (groups.Count + 1));
				var childRequest = context.Request with {
					Mode = "Single",
					TargetVolumeIds = group.TargetVolumeIds,
					Groups = null
				};
				var child = await bindery.ExportSeriesAsync(context.SeriesId, childRequest, cancellationToken);
				if (!child.Success) return ExportResult.Fail($"Group '{group.Name}' failed: {child.ErrorMessage}");

				await using var childStream = child.ContentStream;
				using var bytes = new MemoryStream();
				await childStream.CopyToAsync(bytes, cancellationToken);
				var data = bytes.ToArray();
				var extension = Path.GetExtension(child.FileName);
				var fileName = $"{index + 1:D2}-{SafeName(group.Name)}{extension}";
				var entry = zip.CreateEntry(fileName, CompressionLevel.Optimal);
				await using (var target = entry.Open()) await target.WriteAsync(data, cancellationToken);
				artifacts.Add(new PublishArtifactDto(
					group.Name,
					fileName,
					child.ContentType,
					group.TargetVolumeIds,
					data.LongLength,
					Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant()));
			}

			var manifest = new BatchManifest(
				"grimoire.publish-manifest.v1",
				PrefixedId.ToString(EntityPrefix.Series, series.Id),
				series.Title,
				series.Revision,
				DateTimeOffset.UtcNow,
				context.Request.Mode,
				artifacts);
			var manifestEntry = zip.CreateEntry("manifest.json", CompressionLevel.Optimal);
			await using var manifestStream = manifestEntry.Open();
			await JsonSerializer.SerializeAsync(manifestStream, manifest, cancellationToken: cancellationToken);
		}

		package.Position = 0;
		context.Artifacts = artifacts;
		context.ReportSubProgress(1);
		return ExportResult.Ok(package, $"{SafeName(series.Title)}-package.zip", "application/zip");
	}

	private static string SafeName(string value) {
		var invalid = Path.GetInvalidFileNameChars().ToHashSet();
		var normalized = new string(value.Trim().Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());
		return string.IsNullOrWhiteSpace(normalized) ? "book" : normalized;
	}

	private sealed record BatchManifest(
		string Schema,
		string SeriesId,
		string SeriesTitle,
		long SeriesRevision,
		DateTimeOffset CreatedAt,
		string Mode,
		IReadOnlyList<PublishArtifactDto> Artifacts);
}
