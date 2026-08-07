namespace Grimoire.Application.Service.Pipeline.Ingestion;

using System;
using System.Collections.Generic;
using Grimoire.Application.Dto.Book;
using Grimoire.Domain.Entity.Book;

/// <summary>
///     Context representing the state of an ingestion pipeline run
/// </summary>
public sealed class IngestionContext(Guid volumeId, CreateChapterRequestDto dto) {
	public Guid VolumeId { get; } = volumeId;
	public CreateChapterRequestDto RequestDto { get; } = dto;

	/// <summary>
	///     Series id owning the target volume. Resolved once by the caller (sync loop / bulk import)
	///     so the coordinator does not re-fetch the volume per chapter.
	/// </summary>
	public Guid? SeriesId { get; init; }

	// Entities generated and enriched during the pipeline steps
	public ChapterModel? ExistingChapter { get; set; }
	public ChapterModel Chapter { get; set; } = null!;
	public List<SegmentModel> Segments { get; } = [];
	public SourceMaterial? SourceMaterial { get; set; }

	// Ingestion Audit tracking
	public string SourceType { get; set; } = "Markdown";
	public Guid? AuditRecordId { get; set; }

	/// <summary>
	///     True when this ingestion is one chapter of a bulk import (e.g. EPUB/Markdown via
	///     <see cref="UpsertBulkAsync"/>). Bulk import reconciles asset ownership once for the whole
	///     series at the end, so per-chapter steps that do a full-series scan must be skipped to
	///     avoid O(chapters²) work.
	/// </summary>
	public bool IsBulkImport { get; set; }
}
