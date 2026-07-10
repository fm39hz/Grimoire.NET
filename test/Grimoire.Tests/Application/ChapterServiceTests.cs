namespace Grimoire.Tests.Application;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grimoire.Application.Dto.Book;
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

public class ChapterServiceTests {

	private sealed class Fixture {
		public InMemorySeriesRepository Series { get; }
		public InMemoryVolumeRepository Volumes { get; }
		public InMemoryChapterRepository Chapters { get; }
		public InMemorySegmentRepository Segments { get; } = new();
		public InMemorySourceMaterialRepository Sources { get; } = new();
		public BookTreeService BookTree { get; }
		public ChapterService Service { get; }

		public Fixture() {
			Segments = new InMemorySegmentRepository();
			Chapters = new InMemoryChapterRepository(Segments);
			Volumes = new InMemoryVolumeRepository(Chapters, Segments);
			Series = new InMemorySeriesRepository(Volumes, Chapters, Segments);
			var mapper = new FakeBookMapper();
			BookTree = new BookTreeService(Series, Volumes, Chapters, new NoOpUnitOfWork(), mapper);
			var strategyFactory = new IngestionStrategyFactory([
				new PreProcessedIngestionStrategy(),
				new RawMarkdownIngestionStrategy(Volumes)
			]);
			var unitOfWork = new NoOpUnitOfWork();
			var ingestionCoordinator = new IngestionCoordinator(
				[
					new ParseContentStep(strategyFactory),
					new PersistenceStep(Chapters, Volumes, Segments, Sources),
					new LcaOwnershipStep(new FakeAssetOwnershipService())
				],
				unitOfWork,
				Volumes,
				new InMemoryIngestionAuditRepository()
			);
			Service = new ChapterService(Chapters, Volumes, Sources, Segments, BookTree, mapper, strategyFactory, unitOfWork, ingestionCoordinator);
		}

		private sealed class FakeAssetOwnershipService : IAssetOwnershipService {
			public Task ReconcileSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default) => Task.CompletedTask;
		}



		public async Task<(Guid SeriesId, Guid VolumeId)> SeedSeriesAndVolume() {
			var seriesId = Guid.CreateVersion7();
			var volumeId = Guid.CreateVersion7();

			await Series.Create(new SeriesModel {
				Id = seriesId,
				Title = "Test Series",
				Path = $"n{seriesId:N}"
			});

			await Volumes.Create(new VolumeModel {
				Id = volumeId,
				Order = 1,
				Title = "Test Volume",
				Path = $"n{seriesId:N}.n{volumeId:N}"
			});

			return (seriesId, volumeId);
		}
	}

	private sealed class FakeBookMapper : IBookMapper {
		public SeriesModel CreateSeries(CreateSeriesRequestDto dto) => throw new NotSupportedException();
		public VolumeModel CreateVolume(CreateVolumeRequestDto dto) => throw new NotSupportedException();
		public ChapterModel CreateChapter(CreateChapterRequestDto dto) =>
			new() { Order = dto.Order, Title = dto.Title };

		public void UpdateChapter(UpdateChapterRequestDto dto, ChapterModel model) {
			if (dto.Title is not null) {
				model.Title = dto.Title;
			}

			if (dto.Order is not null) {
				model.Order = dto.Order.Value;
			}
		}

		public void UpdateSeries(UpdateSeriesRequestDto dto, SeriesModel model) => throw new NotSupportedException();
		public void UpdateVolume(UpdateVolumeRequestDto dto, VolumeModel model) => throw new NotSupportedException();

		public ChapterResponseDto ToChapterDto(ChapterModel model) => throw new NotSupportedException();
		public ChapterResponseDto ToChapterDto(ChapterModel model, IEnumerable<SegmentModel> segments) => throw new NotSupportedException();
		public ChapterListResponseDto ToChapterListDto(ChapterModel model) => throw new NotSupportedException();
		public SeriesResponseDto ToSeriesDto(SeriesModel model) => throw new NotSupportedException();
		public VolumeResponseDto ToVolumeDto(VolumeModel model) => throw new NotSupportedException();
		public AssetResponseDto ToAssetDto(AssetModel model) => throw new NotSupportedException();
		public IngestionAuditResponseDto ToIngestionAuditDto(IngestionAuditRecord model) => throw new NotSupportedException();
		public TextSegmentDto ToTextSegmentDto(TextSegmentModel model) => throw new NotSupportedException();
		public IQueryable<VolumeResponseDto> ProjectToVolumeDto(IQueryable<VolumeModel> query) => throw new NotSupportedException();
		public IQueryable<ChapterListResponseDto> ProjectToChapterListDto(IQueryable<ChapterModel> query) => throw new NotSupportedException();
	}

	[Fact]
	public async Task FindOne_ReturnsChapter_IfFound() {
		var fixture = new Fixture();
		var chapter = new ChapterModel { Id = Guid.CreateVersion7(), Path = "n1.n2.n3", Order = 1, Title = "Ch 1" };
		await fixture.Chapters.Create(chapter);

		var result = await fixture.Service.FindOne(chapter.Id);

		Assert.NotNull(result);
		Assert.Equal(chapter.Id, result.Id);
	}

	[Fact]
	public async Task FindOne_ReturnsNull_IfNotFound() {
		var fixture = new Fixture();
		var result = await fixture.Service.FindOne(Guid.NewGuid());
		Assert.Null(result);
	}

	[Fact]
	public async Task Create_CreatesChapterAndSegments() {
		var fixture = new Fixture();
		var (seriesId, volumeId) = await fixture.SeedSeriesAndVolume();

		var dto = new CreateChapterRequestDto(
			VolumeId: PrefixedId.ToString(EntityPrefix.Volume, volumeId),
			Order: 1,
			Title: "New Chapter",
			Content: [
				new TextSegmentModel { Runs = [new TextRun("Paragraph 1")] }
			],
			Footnotes: [],
			RawContent: null
		);

		var result = await fixture.Service.Create(dto);

		Assert.NotNull(result);
		Assert.Equal("New Chapter", result.Title);
		Assert.Equal(1, result.Order);

		var segments = (await fixture.Segments.FindByChapterPath(result.Path)).ToList();
		Assert.Single(segments);
		Assert.Equal("Paragraph 1", ((TextSegmentModel)segments[0]).Runs.First().Text);
	}

	[Fact]
	public async Task Update_UpdatesChapterFields() {
		var fixture = new Fixture();
		var (seriesId, volumeId) = await fixture.SeedSeriesAndVolume();

		var chapter = new ChapterModel {
			Id = Guid.CreateVersion7(),
			Path = $"n{seriesId:N}.n{volumeId:N}.n3",
			Order = 1,
			Title = "Old Title"
		};
		await fixture.Chapters.Create(chapter);

		var dto = new UpdateChapterRequestDto(Order: 2, Title: "New Title", Content: null, Footnotes: null, VolumeId: null);
		var result = await fixture.Service.Update(chapter.Id, dto);

		Assert.Equal("New Title", result.Title);
		Assert.Equal(2, result.Order);
	}

	[Fact]
	public async Task Delete_DeletesChapterAndSegments() {
		var fixture = new Fixture();
		var (seriesId, volumeId) = await fixture.SeedSeriesAndVolume();
		var chapterId = Guid.CreateVersion7();

		var chapter = new ChapterModel {
			Id = chapterId,
			Path = $"n{seriesId:N}.n{volumeId:N}.n{chapterId:N}",
			Order = 1,
			Title = "To Delete"
		};
		await fixture.Chapters.Create(chapter);

		await fixture.Segments.Create(new TextSegmentModel {
			Id = Guid.NewGuid(),
			Path = $"{chapter.Path}.ns1",
			Runs = [new TextRun("Content")]
		});

		await fixture.Service.Delete(chapterId);

		var ch = await fixture.Service.FindOne(chapterId);
		Assert.Null(ch);

		var segs = await fixture.Segments.FindByChapterPath(chapter.Path);
		Assert.Empty(segs);
	}
}
