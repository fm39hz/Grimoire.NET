namespace Grimoire.Application.Ingestion.Legacy;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Contract;
using Domain.Common;
using Domain.Entity.Ingestion;
using Dto.Book;
using Import;
using Reconciliation;

public sealed class LegacySourcePackageAdapter : ILegacySourcePackageAdapter {
	public SourcePackageDto FromSeriesSync(Guid seriesId, SyncSeriesRequestDto request) {
		var serialized = JsonSerializer.Serialize(request);
		var hash = Hash(serialized);
		return new SourcePackageDto(
			"1.0",
			new SourceProducerDto("grimoire.legacy.series-sync", "1"),
			$"sync:{seriesId:N}:{hash}",
			new SourceDescriptorDto($"series-sync:{seriesId:N}", "legacy-series-sync", ObservedAt: DateTimeOffset.UtcNow),
			new TargetHintDto(PrefixedId.ToString(EntityPrefix.Series, seriesId)),
			ImportSemantics.Patch,
			request.Volumes.Select((volume, volumeIndex) => new SourceNodeDto(
				$"volume:{volumeIndex}:{TitleNormalizer.Normalize(volume.Title)}",
				ReconciliationNodeKind.Container,
				volume.Title,
				RoleHint: "volume",
				OrderHint: volume.Order,
				Children: volume.Chapters.Select((chapter, chapterIndex) => new SourceNodeDto(
					$"volume:{volumeIndex}:chapter:{chapterIndex}:{TitleNormalizer.Normalize(chapter.Title)}",
					ReconciliationNodeKind.Content,
					chapter.Title,
					RoleHint: "chapter",
					OrderHint: chapter.Order,
					Content: new SourceContentDto(
						chapter.RawContent is null ? SourceContentFormat.Segments : SourceContentFormat.Markdown,
						Inline: chapter.RawContent ?? JsonSerializer.Serialize(chapter.Content),
						ContentHash: Hash(chapter.RawContent ?? JsonSerializer.Serialize(chapter.Content)),
						Footnotes: chapter.Footnotes))).ToList())).ToList());
	}

	public SourcePackageDto FromEpub(Guid seriesId, NormalizedImport normalized, IReadOnlyList<NormalizedVolume> volumes, string jobId) => new(
		"1.0",
		new SourceProducerDto("grimoire.legacy.epub", "1"),
		$"epub-job:{jobId}",
		new SourceDescriptorDto($"epub-job:{jobId}", "legacy-epub", ObservedAt: DateTimeOffset.UtcNow,
			Metadata: new Dictionary<string, JsonElement> {
				["title"] = JsonSerializer.SerializeToElement(normalized.Title),
				["author"] = JsonSerializer.SerializeToElement(normalized.Author)
			}),
		new TargetHintDto(PrefixedId.ToString(EntityPrefix.Series, seriesId), Titles: [normalized.Title],
			Authors: string.IsNullOrWhiteSpace(normalized.Author) ? [] : [normalized.Author]),
		ImportSemantics.Patch,
		volumes.Select((volume, volumeIndex) => new SourceNodeDto(
			$"volume:{volumeIndex}:{TitleNormalizer.Normalize(volume.Title)}",
			ReconciliationNodeKind.Container,
			volume.Title,
			RoleHint: "volume",
			OrderHint: volume.Order,
			Children: volume.Chapters.Select((chapter, chapterIndex) => new SourceNodeDto(
				$"volume:{volumeIndex}:chapter:{chapterIndex}:{TitleNormalizer.Normalize(chapter.Title)}",
				ReconciliationNodeKind.Content,
				chapter.Title,
				RoleHint: "chapter",
				OrderHint: chapter.Order,
				Content: new SourceContentDto(SourceContentFormat.Segments,
					Inline: JsonSerializer.Serialize(chapter.Segments),
					ContentHash: Hash(JsonSerializer.Serialize(chapter.Segments))))).ToList())).ToList());

	private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
