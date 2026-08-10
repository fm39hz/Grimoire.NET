namespace Grimoire.Application.Ingestion.Contract;

using System.Text.Json;
using Domain.Entity.Ingestion;
using Dto.Book;
using Reconciliation;

public sealed record SourcePackageDto(
	string SchemaVersion,
	SourceProducerDto Producer,
	string IdempotencyKey,
	SourceDescriptorDto Source,
	TargetHintDto? TargetHint,
	ImportSemantics Semantics,
	IReadOnlyList<SourceNodeDto> Nodes,
	IReadOnlyList<SourceAssetDto>? Assets = null);

public sealed record SourceProducerDto(string Id, string Version);

public sealed record SourceDescriptorDto(
	string ExternalKey,
	string Provider,
	string? Uri = null,
	DateTimeOffset? ObservedAt = null,
	IReadOnlyDictionary<string, JsonElement>? Metadata = null);

public sealed record TargetHintDto(
	string? SeriesId = null,
	string? ProducerSeriesKey = null,
	IReadOnlyList<string>? Titles = null,
	IReadOnlyList<string>? Authors = null);

public sealed record SourceNodeDto(
	string ExternalKey,
	ReconciliationNodeKind Kind,
	string Title,
	string? TargetId = null,
	string? LogicalKey = null,
	string? RoleHint = null,
	double? OrderHint = null,
	SourceContentDto? Content = null,
	IReadOnlyList<SourceRelationDto>? Relations = null,
	IReadOnlyList<SourceNodeDto>? Children = null,
	IReadOnlyDictionary<string, JsonElement>? Metadata = null);

public enum SourceContentFormat {
	Markdown,
	Html,
	Segments
}

public sealed record SourceContentDto(
	SourceContentFormat Format,
	string? Inline = null,
	string? StagingObjectKey = null,
	string? ContentHash = null,
	IReadOnlyList<ImportFootnoteDto>? Footnotes = null);

public sealed record SourceRelationDto(
	string Type,
	string? TargetExternalKey = null,
	string? Uri = null,
	IReadOnlyDictionary<string, JsonElement>? Metadata = null);

public sealed record SourceAssetDto(
	string ExternalKey,
	string FileName,
	string MediaType,
	string StagingObjectKey,
	string ContentHash,
	string? RoleHint = null);
