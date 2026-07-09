namespace Grimoire.Application.Service.Strategy;

using Domain.Entity.Book;

/// <summary>
///     Result of chapter ingestion containing the created entities
/// </summary>
public record IngestionResult(
	ChapterModel Chapter,
	IReadOnlyList<SegmentModel> Segments,
	SourceMaterial? Source);
