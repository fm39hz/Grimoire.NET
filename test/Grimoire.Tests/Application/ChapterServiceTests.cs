namespace Grimoire.Tests.Application;

using System.Linq;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Dto.Common;
using Grimoire.Application.Mapper;
using Grimoire.Application.Service.Implementation;
using Grimoire.Application.Service.Strategy;
using Grimoire.Domain.Common;
using Grimoire.Domain.Common.Repository;
using Grimoire.Domain.Entity.Book;
using Grimoire.Domain.Entity.Book.Segment;
using Grimoire.Domain.Exception;
using Grimoire.Tests.TestInfrastructure;
using Xunit;

public sealed class ChapterServiceTests {
	// ── Fixture ───────────────────────────────────────────────────────────────

	private sealed class Fixture {
		public InMemoryBookTreeRepository Tree { get; } = new();
		public InMemorySeriesRepository Series { get; } = new();
		public InMemoryVolumeRepository Volumes { get; } = new();
		public InMemoryChapterRepository Chapters { get; } = new();
		public InMemorySourceMaterialRepository Sources { get; } = new();
		public BookTreeService BookTree { get; }
		public ChapterService Service { get; }

		public Fixture() {
			var mapper = new FakeBookMapper();
			BookTree = new BookTreeService(Tree, Series, Volumes, Chapters, mapper);
			var strategyFactory = new IngestionStrategyFactory([
				new PreProcessedIngestionStrategy(),
				new RawMarkdownIngestionStrategy(Volumes)
			]);
			var unitOfWork = new NoOpUnitOfWork();
			Service = new ChapterService(Chapters, Volumes, Sources, BookTree, mapper, strategyFactory, unitOfWork);
		}

		public async Task<(Guid SeriesId, Guid VolumeId)> SeedSeriesAndVolume() {
			var seriesId = Guid.CreateVersion7();
			var volumeId = Guid.CreateVersion7();

			// Add to tree
			await Tree.Create(new BookNodeModel {
				Id = seriesId,
				Type = BookNodeType.Series,
				ParentId = null,
				Title = "Test Series",
				Order = 1
			});
			await Tree.Create(new BookNodeModel {
				Id = volumeId,
				Type = BookNodeType.Volume,
				ParentId = seriesId,
				Title = "Test Volume",
				Order = 1
			});

			// Add to volume repo
			await Volumes.Create(new VolumeModel {
				Id = volumeId,
				SeriesId = seriesId,
				Order = 1,
				Title = "Test Volume"
			});

			return (seriesId, volumeId);
		}
	}

	// ── Fake Mapper ───────────────────────────────────────────────────────────

	private sealed class FakeBookMapper : IBookMapper {
		public SeriesModel CreateSeries(CreateSeriesRequestDto dto) => throw new NotSupportedException();
		public VolumeModel CreateVolume(CreateVolumeRequestDto dto, Guid seriesId) => throw new NotSupportedException();
		public ChapterModel CreateChapter(CreateChapterRequestDto dto, Guid volumeId) =>
			new() { VolumeId = volumeId, Order = dto.Order, Title = dto.Title };

		public void UpdateChapter(UpdateChapterRequestDto dto, ChapterModel model) {
			if (dto.Title is not null) model.Title = dto.Title;
			if (dto.Order is not null) model.Order = dto.Order.Value;
		}

		public void UpdateSeries(UpdateSeriesRequestDto dto, SeriesModel model) => throw new NotSupportedException();
		public void UpdateVolume(UpdateVolumeRequestDto dto, VolumeModel model) => throw new NotSupportedException();
		public ChapterResponseDto ToChapterDto(ChapterModel model) => throw new NotSupportedException();
		public ChapterListResponseDto ToChapterListDto(ChapterModel model) => throw new NotSupportedException();
		public SeriesResponseDto ToSeriesDto(SeriesModel model) => throw new NotSupportedException();
		public VolumeResponseDto ToVolumeDto(VolumeModel model) => throw new NotSupportedException();
		public AssetResponseDto ToAssetDto(AssetModel model) => throw new NotSupportedException();
	}

	// ── FindOne & FindAll Tests ───────────────────────────────────────────────

	[Fact]
	public async Task FindOne_ReturnsChapter_IfFound() {
		var fixture = new Fixture();
		var chapter = new ChapterModel { Id = Guid.CreateVersion7(), VolumeId = Guid.NewGuid(), Order = 1, Title = "Ch 1" };
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
	public async Task FindAll_ReturnsPagedResult() {
		var fixture = new Fixture();
		var volId = Guid.NewGuid();
		await fixture.Chapters.Create(new ChapterModel { Id = Guid.CreateVersion7(), VolumeId = volId, Order = 1, Title = "C1" });
		await fixture.Chapters.Create(new ChapterModel { Id = Guid.CreateVersion7(), VolumeId = volId, Order = 2, Title = "C2" });

		var result = await fixture.Service.FindAll(new PaginationRequest { PageIndex = 1, PageSize = 1 });

		Assert.Equal(2, result.TotalCount);
		Assert.Single(result.Items);
	}

	// ── Create Tests ──────────────────────────────────────────────────────────

	[Fact]
	public async Task Create_VolumeNotFound_ThrowsEntityNotFoundException() {
		var fixture = new Fixture();
		var nonExistentVolumeIdStr = PrefixedId.ToString(EntityPrefix.Volume, Guid.NewGuid());
		var dto = new CreateChapterRequestDto(
			VolumeId: nonExistentVolumeIdStr,
			Order: 1,
			Title: "New Chapter",
			Content: [],
			Footnotes: [],
			RawContent: null
		);

		await Assert.ThrowsAsync<EntityNotFoundException>(() => fixture.Service.Create(dto));
	}

	[Fact]
	public async Task Create_ValidVolume_CreatesChapterAndNode() {
		var fixture = new Fixture();
		var (_, volumeId) = await fixture.SeedSeriesAndVolume();
		var volumeIdStr = PrefixedId.ToString(EntityPrefix.Volume, volumeId);

		var dto = new CreateChapterRequestDto(
			VolumeId: volumeIdStr,
			Order: 1,
			Title: "Preprocessed Ch",
			Content: [],
			Footnotes: [],
			RawContent: null
		);

		var chapter = await fixture.Service.Create(dto);

		Assert.NotNull(chapter);
		Assert.Equal("Preprocessed Ch", chapter.Title);
		Assert.Equal(1, chapter.Order);

		// Assert persisted in repo
		var savedChapter = await fixture.Chapters.FindOne(chapter.Id);
		Assert.NotNull(savedChapter);

		// Assert node created in tree
		var treeNode = await fixture.Tree.FindOne(chapter.Id);
		Assert.NotNull(treeNode);
		Assert.Equal(BookNodeType.Chapter, treeNode.Type);
		Assert.Equal(volumeId, treeNode.ParentId);
	}

	// ── UpsertAsync Tests ─────────────────────────────────────────────────────

	[Fact]
	public async Task UpsertAsync_NewChapter_CreatesAndSavesChapterAndTreeNode() {
		var fixture = new Fixture();
		var (_, volumeId) = await fixture.SeedSeriesAndVolume();

		var dto = new CreateChapterRequestDto(
			VolumeId: PrefixedId.ToString(EntityPrefix.Volume, volumeId),
			Order: 3,
			Title: "Upserted New",
			Content: [new TextSegmentModel { Id = Guid.NewGuid(), Runs = [new TextRun("hello")] }],
			Footnotes: [],
			RawContent: null
		);

		var (chapter, created) = await fixture.Service.UpsertAsync(volumeId, dto);

		Assert.True(created);
		Assert.NotNull(chapter);

		var repoChapter = await fixture.Chapters.FindOne(chapter.Id);
		Assert.NotNull(repoChapter);
		Assert.Single(repoChapter.ContentData!.Segments);

		var treeNode = await fixture.Tree.FindOne(chapter.Id);
		Assert.NotNull(treeNode);
		Assert.Equal("Upserted New", treeNode.Title);
	}

	[Fact]
	public async Task UpsertAsync_ExistingChapter_UpdatesChapterAndTreeNode() {
		var fixture = new Fixture();
		var (_, volumeId) = await fixture.SeedSeriesAndVolume();

		var existingChapter = new ChapterModel {
			Id = Guid.CreateVersion7(),
			VolumeId = volumeId,
			Order = 2,
			Title = "Original Upsert Title",
			ContentData = new ChapterContentModel { Segments = [], Footnotes = [] }
		};
		await fixture.Chapters.Create(existingChapter);
		await fixture.Tree.Create(new BookNodeModel {
			Id = existingChapter.Id,
			Type = BookNodeType.Chapter,
			ParentId = volumeId,
			Title = existingChapter.Title,
			Order = existingChapter.Order
		});

		var dto = new CreateChapterRequestDto(
			VolumeId: PrefixedId.ToString(EntityPrefix.Volume, volumeId),
			Order: 2, // matches existing
			Title: "Updated Upsert Title",
			Content: [new TextSegmentModel { Id = Guid.NewGuid(), Runs = [new TextRun("updated content")] }],
			Footnotes: [],
			RawContent: null
		);

		var (chapter, created) = await fixture.Service.UpsertAsync(volumeId, dto);

		Assert.False(created);
		Assert.Equal(existingChapter.Id, chapter.Id);
		Assert.Equal("Updated Upsert Title", chapter.Title);

		var repoChapter = await fixture.Chapters.FindOne(chapter.Id);
		Assert.Equal("Updated Upsert Title", repoChapter!.Title);
		Assert.Equal("updated content", ((TextSegmentModel)repoChapter.ContentData!.Segments[0]).Runs.First().Text);

		var treeNode = await fixture.Tree.FindOne(chapter.Id);
		Assert.Equal("Updated Upsert Title", treeNode!.Title);
	}

	// ── Update Tests ──────────────────────────────────────────────────────────

	[Fact]
	public async Task Update_ChapterNotFound_ThrowsEntityNotFoundException() {
		var fixture = new Fixture();
		var dto = new UpdateChapterRequestDto(Order: 1, Title: "U", Content: null, Footnotes: null, VolumeId: null);

		await Assert.ThrowsAsync<EntityNotFoundException>(() =>
			fixture.Service.Update(Guid.NewGuid(), dto));
	}

	[Fact]
	public async Task Update_ValidFields_UpdatesChapterAndTreeNode() {
		var fixture = new Fixture();
		var (_, volumeId) = await fixture.SeedSeriesAndVolume();

		var chapter = new ChapterModel { Id = Guid.CreateVersion7(), VolumeId = volumeId, Order = 1, Title = "C1" };
		await fixture.Chapters.Create(chapter);
		await fixture.Tree.Create(new BookNodeModel {
			Id = chapter.Id,
			Type = BookNodeType.Chapter,
			ParentId = volumeId,
			Title = chapter.Title,
			Order = chapter.Order
		});

		var dto = new UpdateChapterRequestDto(Order: 10, Title: "C1 Updated", Content: null, Footnotes: null, VolumeId: null);
		var result = await fixture.Service.Update(chapter.Id, dto);

		Assert.Equal("C1 Updated", result.Title);
		Assert.Equal(10, result.Order);

		var treeNode = await fixture.Tree.FindOne(chapter.Id);
		Assert.Equal("C1 Updated", treeNode!.Title);
		Assert.Equal(10, treeNode.Order);
	}

	[Fact]
	public async Task Update_MoveVolume_MovesNodeInTree() {
		var fixture = new Fixture();
		var (seriesId, volumeIdA) = await fixture.SeedSeriesAndVolume();

		// Add second volume
		var volumeIdB = Guid.CreateVersion7();
		await fixture.Tree.Create(new BookNodeModel {
			Id = volumeIdB,
			Type = BookNodeType.Volume,
			ParentId = seriesId,
			Title = "Test Volume B",
			Order = 2
		});
		await fixture.Volumes.Create(new VolumeModel {
			Id = volumeIdB,
			SeriesId = seriesId,
			Order = 2,
			Title = "Test Volume B"
		});

		// Create chapter under volume A
		var chapter = new ChapterModel { Id = Guid.CreateVersion7(), VolumeId = volumeIdA, Order = 1, Title = "Ch" };
		await fixture.Chapters.Create(chapter);
		await fixture.Tree.Create(new BookNodeModel {
			Id = chapter.Id,
			Type = BookNodeType.Chapter,
			ParentId = volumeIdA,
			Title = chapter.Title,
			Order = chapter.Order
		});

		var dto = new UpdateChapterRequestDto(
			Order: 1,
			Title: "Ch",
			Content: null,
			Footnotes: null,
			VolumeId: PrefixedId.ToString(EntityPrefix.Volume, volumeIdB)
		);

		var result = await fixture.Service.Update(chapter.Id, dto);

		Assert.Equal(volumeIdB, result.VolumeId);

		var treeNode = await fixture.Tree.FindOne(chapter.Id);
		Assert.Equal(volumeIdB, treeNode!.ParentId);
	}

	// ── Delete Tests ──────────────────────────────────────────────────────────

	[Fact]
	public async Task Delete_DeletesSubtreeAndReturnsCount() {
		var fixture = new Fixture();
		var (_, volumeId) = await fixture.SeedSeriesAndVolume();
		var chapter = new ChapterModel { Id = Guid.CreateVersion7(), VolumeId = volumeId, Order = 1, Title = "C1" };
		await fixture.Chapters.Create(chapter);
		await fixture.Tree.Create(new BookNodeModel {
			Id = chapter.Id,
			Type = BookNodeType.Chapter,
			ParentId = volumeId,
			Title = chapter.Title,
			Order = chapter.Order
		});

		var deletedCount = await fixture.Service.Delete(chapter.Id);

		Assert.Equal(1, deletedCount);
		Assert.Null(await fixture.Tree.FindOne(chapter.Id));
	}

	// ── MergeAsync Tests ──────────────────────────────────────────────────────

	[Fact]
	public async Task MergeAsync_LessThanTwoChapters_ThrowsInvalidOperationException() {
		var fixture = new Fixture();
		var dto = new MergeChaptersRequestDto([PrefixedId.ToString(EntityPrefix.Chapter, Guid.NewGuid())]);

		await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.MergeAsync(dto));
	}

	[Fact]
	public async Task MergeAsync_ChaptersFromDifferentVolumes_ThrowsInvalidOperationException() {
		var fixture = new Fixture();
		var (_, volumeIdA) = await fixture.SeedSeriesAndVolume();
		var (_, volumeIdB) = await fixture.SeedSeriesAndVolume();

		var c1 = new ChapterModel { Id = Guid.CreateVersion7(), VolumeId = volumeIdA, Order = 1, Title = "A" };
		var c2 = new ChapterModel { Id = Guid.CreateVersion7(), VolumeId = volumeIdB, Order = 1, Title = "B" };
		await fixture.Chapters.Create(c1);
		await fixture.Chapters.Create(c2);

		var dto = new MergeChaptersRequestDto([
			PrefixedId.ToString(EntityPrefix.Chapter, c1.Id),
			PrefixedId.ToString(EntityPrefix.Chapter, c2.Id)
		]);

		await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.MergeAsync(dto));
	}

	[Fact]
	public async Task MergeAsync_ValidChapters_MergesAndDeletesSources() {
		var fixture = new Fixture();
		var (_, volumeId) = await fixture.SeedSeriesAndVolume();

		var c1 = new ChapterModel {
			Id = Guid.CreateVersion7(),
			VolumeId = volumeId,
			Order = 1,
			Title = "A",
			ContentData = new ChapterContentModel {
				Segments = [new TextSegmentModel { Id = Guid.NewGuid(), Runs = [new TextRun("Segment A")] }],
				Footnotes = []
			}
		};
		var c2 = new ChapterModel {
			Id = Guid.CreateVersion7(),
			VolumeId = volumeId,
			Order = 2,
			Title = "B",
			ContentData = new ChapterContentModel {
				Segments = [new TextSegmentModel { Id = Guid.NewGuid(), Runs = [new TextRun("Segment B")] }],
				Footnotes = []
			}
		};

		await fixture.Chapters.Create(c1);
		await fixture.Chapters.Create(c2);

		await fixture.Tree.Create(new BookNodeModel { Id = c1.Id, Type = BookNodeType.Chapter, ParentId = volumeId, Title = c1.Title, Order = c1.Order });
		await fixture.Tree.Create(new BookNodeModel { Id = c2.Id, Type = BookNodeType.Chapter, ParentId = volumeId, Title = c2.Title, Order = c2.Order });

		var dto = new MergeChaptersRequestDto([
			PrefixedId.ToString(EntityPrefix.Chapter, c1.Id),
			PrefixedId.ToString(EntityPrefix.Chapter, c2.Id)
		]);

		var result = await fixture.Service.MergeAsync(dto);

		Assert.NotNull(result);
		Assert.Equal(c1.Id, result.Id);
		Assert.Equal(2, result.ContentData!.Segments.Count);
		Assert.Equal("Segment A", ((TextSegmentModel)result.ContentData.Segments[0]).Runs.First().Text);
		Assert.Equal("Segment B", ((TextSegmentModel)result.ContentData.Segments[1]).Runs.First().Text);

		// Assert second chapter node is deleted from the tree
		Assert.Null(await fixture.Tree.FindOne(c2.Id));
	}

	// ── SplitAsync Tests ──────────────────────────────────────────────────────

	[Fact]
	public async Task SplitAsync_NoContent_ThrowsInvalidOperationException() {
		var fixture = new Fixture();
		var (_, volumeId) = await fixture.SeedSeriesAndVolume();
		var chapter = new ChapterModel { Id = Guid.CreateVersion7(), VolumeId = volumeId, Order = 1, Title = "Empty" };
		await fixture.Chapters.Create(chapter);

		var dto = new SplitChapterRequestDto([new SplitPointDto(1, "Part 2")]);

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			fixture.Service.SplitAsync(chapter.Id, dto));
	}

	[Fact]
	public async Task SplitAsync_WithContent_SplitsChapterAndCreatesNewNodes() {
		var fixture = new Fixture();
		var (_, volumeId) = await fixture.SeedSeriesAndVolume();

		var chapter = new ChapterModel {
			Id = Guid.CreateVersion7(),
			VolumeId = volumeId,
			Order = 1f,
			Title = "Original Title",
			ContentData = new ChapterContentModel {
				Segments = [
					new TextSegmentModel { Id = Guid.NewGuid(), Runs = [new TextRun("Para 1")] },
					new TextSegmentModel { Id = Guid.NewGuid(), Runs = [new TextRun("Para 2")] },
					new TextSegmentModel { Id = Guid.NewGuid(), Runs = [new TextRun("Para 3")] }
				],
				Footnotes = []
			}
		};
		await fixture.Chapters.Create(chapter);
		await fixture.Tree.Create(new BookNodeModel {
			Id = chapter.Id,
			Type = BookNodeType.Chapter,
			ParentId = volumeId,
			Title = chapter.Title,
			Order = chapter.Order
		});

		var dto = new SplitChapterRequestDto([
			new SplitPointDto(1, "Title Part 2"), // Split at index 1 -> Segments 1 and 2 go to new chapter
		]);

		var result = (await fixture.Service.SplitAsync(chapter.Id, dto)).ToList();

		// original and 1 new chapter -> 2 total returned
		Assert.Equal(2, result.Count);

		var updatedOriginal = result[0];
		var newChapter = result[1];

		// Verify original chapter segment count is 1
		Assert.Single(updatedOriginal.ContentData!.Segments);
		Assert.Equal("Para 1", ((TextSegmentModel)updatedOriginal.ContentData.Segments[0]).Runs.First().Text);

		// Verify new chapter segment count is 2
		Assert.Equal(2, newChapter.ContentData!.Segments.Count);
		Assert.Equal("Para 2", ((TextSegmentModel)newChapter.ContentData.Segments[0]).Runs.First().Text);
		Assert.Equal("Para 3", ((TextSegmentModel)newChapter.ContentData.Segments[1]).Runs.First().Text);

		// Verify tree nodes
		var originalNode = await fixture.Tree.FindOne(updatedOriginal.Id);
		Assert.NotNull(originalNode);
		Assert.Equal(1f, originalNode.Order);

		var newNode = await fixture.Tree.FindOne(newChapter.Id);
		Assert.NotNull(newNode);
		Assert.Equal("Title Part 2", newNode.Title);
		Assert.Equal(1.1f, newNode.Order, precision: 4);
	}
}
