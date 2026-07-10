namespace Grimoire.Tests.Application;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Service.Pipeline.Ingestion;
using Grimoire.Application.Service.Pipeline.Ingestion.Steps;
using Grimoire.Application.Service.Strategy;
using Grimoire.Domain.Common;
using Grimoire.Domain.Entity.Book;
using Grimoire.Tests.TestInfrastructure;
using Xunit;

public sealed class PipelineTests {

	[Fact]
	public async Task IngestionPipeline_ExecutesAllStepsSuccessfully() {
		// Arrange
		var volumeId = Guid.CreateVersion7();
		var dto = new CreateChapterRequestDto(
			VolumeId: PrefixedId.ToString(EntityPrefix.Volume, volumeId),
			Order: 1,
			Title: "Pipeline Ingested Chapter",
			Content: [],
			Footnotes: [],
			RawContent: "Raw content"
		);

		var context = new IngestionContext(volumeId, dto);
		var uow = new NoOpUnitOfWork();
		var chapters = new InMemoryChapterRepository();
		var volumes = new InMemoryVolumeRepository(chapters);
		var segments = new InMemorySegmentRepository();
		var sources = new InMemorySourceMaterialRepository();

		await volumes.Create(new VolumeModel {
			Id = volumeId,
			Order = 1,
			Title = "Vol 1",
			Path = $"n{Guid.CreateVersion7():N}.n{volumeId:N}"
		});

		var strategyFactory = new IngestionStrategyFactory([
			new RawMarkdownIngestionStrategy(volumes)
		]);

		var steps = new List<IIngestionPipelineStep> {
			new ParseContentStep(strategyFactory),
			new PersistenceStep(chapters, volumes, segments, sources)
		};

		var coordinator = new IngestionCoordinator(steps, uow);

		// Act
		await coordinator.ExecuteAsync(context);

		// Assert
		Assert.NotNull(context.Chapter);
		Assert.Equal("Pipeline Ingested Chapter", context.Chapter.Title);
		Assert.EndsWith(context.Chapter.Id.ToString("N"), context.Chapter.Path.Value);

		var savedChapter = await chapters.FindOne(context.Chapter.Id);
		Assert.NotNull(savedChapter);
	}
}
