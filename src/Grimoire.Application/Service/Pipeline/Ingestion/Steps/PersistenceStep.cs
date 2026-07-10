namespace Grimoire.Application.Service.Pipeline.Ingestion.Steps;

using System;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Domain.Common.Repository;

public sealed class PersistenceStep(
	IChapterRepository chapterRepository,
	IVolumeRepository volumeRepository,
	ISegmentRepository segmentRepository,
	ISourceMaterialRepository sourceRepository) : IIngestionPipelineStep {

	public int ExecutionOrder => 20; // Runs before LCA (30)

	public async Task ExecuteAsync(IngestionContext context, CancellationToken cancellationToken) {
		if (context.SourceMaterial != null) {
			await sourceRepository.Create(context.SourceMaterial, cancellationToken);
		}

		if (context.ExistingChapter != null) {
			var existing = context.ExistingChapter;
			existing.Title = context.Chapter.Title;
			existing.Status = context.Chapter.Status;
			existing.Order = context.Chapter.Order;

			// Re-map segment paths to ensure they match the existing chapter's canonical path
			await segmentRepository.DeleteByChapterPath(existing.Path, cancellationToken);
			foreach (var seg in context.Segments) {
				seg.Path = $"{existing.Path.Value}.n{seg.Id:N}";
			}
			await segmentRepository.CreateBulk(context.Segments, cancellationToken);
			await chapterRepository.Update(existing, cancellationToken);

			context.Chapter = existing;
		}
		else {
			// Creating a new chapter
			if (context.Chapter.Path == null || string.IsNullOrEmpty(context.Chapter.Path.Value)) {
				var volume = await volumeRepository.FindOne(context.VolumeId, cancellationToken) ??
					throw new InvalidOperationException($"Volume with ID {context.VolumeId} not found");
				context.Chapter.Path = $"{volume.Path.Value}.n{context.Chapter.Id:N}";
			}

			await chapterRepository.Create(context.Chapter, cancellationToken);

			// Map segment paths to the new chapter's path
			foreach (var seg in context.Segments) {
				seg.Path = $"{context.Chapter.Path.Value}.n{seg.Id:N}";
			}
			await segmentRepository.CreateBulk(context.Segments, cancellationToken);
		}
	}
}
