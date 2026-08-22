namespace Grimoire.Tests.Application;

using System;
using System.Linq;
using System.Threading.Tasks;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Dto.Book.Restructure;
using Grimoire.Application.Dto.Book.Segment;
using Grimoire.Application.Mapper;
using Grimoire.Application.Service.Contract;
using Grimoire.Application.Service.Implementation;
using Grimoire.Application.Service.Pipeline.Ingestion;
using Grimoire.Application.Service.Pipeline.Ingestion.Steps;
using Grimoire.Application.Service.Strategy;
using Grimoire.Domain.Common;
using Grimoire.Domain.Entity.Book;
using Grimoire.Domain.Entity.Book.Segment;
using Grimoire.Tests.TestInfrastructure;
using Xunit;

public sealed class BookRestructureTests {

	[Fact]
	public async Task MergeVolumes_MovesChaptersAndDeletesTarget() {
		var fx = Fixture.Create();
		var series = await fx.SeedSeries();
		var v1 = await fx.CreateVolume(series, order: 1, "Vol 1");
		var v2 = await fx.CreateVolume(series, order: 2, "Vol 2");
		var c1 = await fx.CreateChapter(v1, order: 1, "Ch 1");
		var c2 = await fx.CreateChapter(v2, order: 1, "Ch 2");

		await fx.BookTree.MergeVolumesAsync(v1.Id, [v2.Id], default);

		// Chapter 2 moved into volume 1; volume 2 deleted.
		var ch2 = fx.Chapters.Items.Single(c => c.Id == c2.Id);
		Assert.Equal(v1.Id, ch2.Path.GetVolumeId());
		Assert.DoesNotContain(fx.Volumes.Items, v => v.Id == v2.Id);
		Assert.Single(fx.Volumes.Items);
	}

	[Fact]
	public async Task SplitVolume_MovesChaptersAtBoundary_ToNewVolume() {
		var fx = Fixture.Create();
		var series = await fx.SeedSeries();
		var v1 = await fx.CreateVolume(series, order: 1, "Vol 1");
		var c1 = await fx.CreateChapter(v1, order: 1, "Ch 1");
		var c2 = await fx.CreateChapter(v1, order: 2, "Ch 2");

		var newVolume = await fx.BookTree.SplitVolumeAsync(v1.Id, atChapterOrder: 2, "Vol 1b", default);

		Assert.Equal(2, fx.Volumes.Items.Count);
		var moved = fx.Chapters.Items.Single(c => c.Id == c2.Id);
		Assert.Equal(newVolume.Id, moved.Path.GetVolumeId());
		Assert.Equal(v1.Id, fx.Chapters.Items.Single(c => c.Id == c1.Id).Path.GetVolumeId());
	}

	[Fact]
	public async Task ReorderSiblings_AssignsSequentialOrders() {
		var fx = Fixture.Create();
		var series = await fx.SeedSeries();
		var v1 = await fx.CreateVolume(series, order: 5, "Vol A");
		var v2 = await fx.CreateVolume(series, order: 2, "Vol B");
		var v3 = await fx.CreateVolume(series, order: 9, "Vol C");

		await fx.BookTree.ReorderSiblingsAsync(series.Id, [v2.Id, v1.Id, v3.Id], default);

		Assert.Equal(1, fx.Volumes.Items.Single(v => v.Id == v2.Id).Order);
		Assert.Equal(2, fx.Volumes.Items.Single(v => v.Id == v1.Id).Order);
		Assert.Equal(3, fx.Volumes.Items.Single(v => v.Id == v3.Id).Order);
	}

	[Fact]
	public async Task UpdateSegmentText_RejectsNonTextSegment() {
		var fx = Fixture.Create();
		var series = await fx.SeedSeries();
		var v1 = await fx.CreateVolume(series, order: 1, "Vol 1");
		var ch = await fx.CreateChapter(v1, order: 1, "Ch 1");
		var img = new ImageSegmentModel {
			Id = Guid.NewGuid(),
			AssetKey = "ast_11111111-1111-1111-1111-111111111111",
			Order = 1
		};
		fx.Segments.Items.Add(img);

		var svc = new SegmentService(fx.Segments);

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			svc.UpdateTextAsync(img.Id, [new TextRunDto("x")], default));
	}

	[Fact]
	public async Task UpdateSegmentText_UpdatesTextRuns() {
		var fx = Fixture.Create();
		var series = await fx.SeedSeries();
		var v1 = await fx.CreateVolume(series, order: 1, "Vol 1");
		var ch = await fx.CreateChapter(v1, order: 1, "Ch 1");
		var text = new TextSegmentModel {
			Id = Guid.NewGuid(),
			Runs = [new TextRun("old")],
			Order = 1
		};
		fx.Segments.Items.Add(text);

		var svc = new SegmentService(fx.Segments);
		var updated = await svc.UpdateTextAsync(text.Id, [new TextRunDto("new", IsBold: true)], default);

		var run = Assert.Single(updated.Runs);
		Assert.Equal("new", run.Text);
		Assert.True(run.IsBold);
	}

	[Fact]
	public async Task Orchestrator_ExecutesBatchAndRollsBackOnError() {
		var fx = Fixture.Create();
		var series = await fx.SeedSeries();
		var v1 = await fx.CreateVolume(series, order: 1, "Vol 1");
		var v2 = await fx.CreateVolume(series, order: 2, "Vol 2");
		var c1 = await fx.CreateChapter(v1, order: 1, "Ch 1");

		var svc = new BookRestructureService(
			fx.BookTree,
			fx.ChapterService,
			new SegmentService(fx.Segments),
			new NoOpUnitOfWork());

		var request = new BookRestructureRequestDto(
			PrefixedId.ToString(EntityPrefix.Series, series.Id),
			[
				new MergeVolumesOp(
					PrefixedId.ToString(EntityPrefix.Volume, v1.Id),
					[PrefixedId.ToString(EntityPrefix.Volume, v2.Id)]),
				// Invalid: base volume is also a target — should roll back the whole batch.
				new MergeVolumesOp(
					PrefixedId.ToString(EntityPrefix.Volume, v1.Id),
					[PrefixedId.ToString(EntityPrefix.Volume, v1.Id)])
			]);

		// Second op fails at ValidateOp; batch is pre-validated before any mutation.
		var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ExecuteAsync(request, default));
		Assert.Contains("Base volume", ex.Message);
		// Nothing mutated: volume 2 still exists, chapter 1 unchanged.
		Assert.Equal(2, fx.Volumes.Items.Count);
		Assert.Equal(v1.Id, fx.Chapters.Items.Single(c => c.Id == c1.Id).Path.GetVolumeId());
	}

	private sealed record Fixture(
		BookTreeService BookTree,
		InMemorySeriesRepository Series,
		InMemoryVolumeRepository Volumes,
		InMemoryChapterRepository Chapters,
		InMemorySegmentRepository Segments,
		IChapterService ChapterService) {
		public static Fixture Create() {
			var segments = new InMemorySegmentRepository();
			var chapters = new InMemoryChapterRepository(segments);
			var volumes = new InMemoryVolumeRepository(chapters, segments);
			var series = new InMemorySeriesRepository(volumes, chapters, segments);
			var mapper = new BookMapper();

			var bookTree = new BookTreeService(series, volumes, chapters, new NoOpUnitOfWork(), mapper);

			var chapterService = new ChapterService(
				chapters,
				volumes,
				new InMemorySourceMaterialRepository(),
				segments,
				bookTree,
				mapper,
				new IngestionStrategyFactory([new PreProcessedIngestionStrategy(mapper), new RawMarkdownIngestionStrategy(new MarkdownSegmentParser(), volumes)]),
				new NoOpUnitOfWork(),
				new IngestionCoordinator(
					[
						new ParseContentStep(new IngestionStrategyFactory([new PreProcessedIngestionStrategy(mapper), new RawMarkdownIngestionStrategy(new MarkdownSegmentParser(), volumes)])),
						new PersistenceStep(chapters, volumes, segments, new InMemorySourceMaterialRepository()),
						new LcaOwnershipStep(new FakeAssetOwnershipService())
					],
					new NoOpUnitOfWork(),
					volumes,
					new InMemoryIngestionAuditRepository()));

			return new Fixture(bookTree, series, volumes, chapters, segments, chapterService);
		}

		public async Task<SeriesModel> SeedSeries() {
			var seriesId = Guid.NewGuid();
			var series = new SeriesModel {
				Id = seriesId,
				Title = "Series",
				Path = "n" + seriesId.ToString("N")
			};
			await Series.Create(series);
			return series;
		}

		public async Task<VolumeModel> CreateVolume(SeriesModel series, double order, string title) {
			var volumeId = Guid.NewGuid();
			var volume = new VolumeModel {
				Id = volumeId,
				Title = title,
				Order = order,
				Path = $"{series.Path}.n{volumeId:N}"
			};
			await Volumes.Create(volume);
			return volume;
		}

		public async Task<ChapterModel> CreateChapter(VolumeModel volume, double order, string title) {
			var chapterId = Guid.NewGuid();
			var chapter = new ChapterModel {
				Id = chapterId,
				Title = title,
				Order = order,
				Path = $"{volume.Path}.n{chapterId:N}"
			};
			await Chapters.Create(chapter);
			return chapter;
		}
	}

	private sealed class FakeAssetOwnershipService : IAssetOwnershipService {
		public Task ReconcileSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default) => Task.CompletedTask;
	}
}
