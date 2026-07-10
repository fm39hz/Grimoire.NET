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

	// Entities generated and enriched during the pipeline steps
	public ChapterModel? ExistingChapter { get; set; }
	public ChapterModel Chapter { get; set; } = null!;
	public List<SegmentModel> Segments { get; } = [];
	public SourceMaterial? SourceMaterial { get; set; }
}
