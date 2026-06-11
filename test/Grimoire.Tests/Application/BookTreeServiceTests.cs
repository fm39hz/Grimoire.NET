namespace Grimoire.Tests.Application;

using Grimoire.Application.Dto.Book;
using Grimoire.Application.Dto.Book.Metadata;
using Grimoire.Application.Dto.Book.Segment;
using Grimoire.Application.Dto.Book.Tree;
using Grimoire.Application.Mapper;
using Grimoire.Application.Service.Implementation;
using Grimoire.Domain.Common;
using Grimoire.Domain.Common.Repository;
using Grimoire.Domain.Entity;
using Grimoire.Domain.Entity.Book;
using Grimoire.Domain.Entity.Book.Metadata;
using Grimoire.Domain.Entity.Book.Segment;
using Xunit;

public sealed class BookTreeServiceTests {
	[Fact]
	public async Task GetTree_ReturnsShelfRootWithOrderedSeriesVolumeChapterHierarchy() {
		var fixture = Fixture.Create();
		var series = await fixture.Service.CreateSeries(new CreateSeriesRequestDto("Series", new SeriesMetadataDto()));
		var volume2 = await fixture.Service.CreateVolume(new CreateVolumeRequestDto(PrefixedId.ToString(EntityPrefix.Series, series.Id), 2, "Volume 2", null));
		var volume1 = await fixture.Service.CreateVolume(new CreateVolumeRequestDto(PrefixedId.ToString(EntityPrefix.Series, series.Id), 1, "Volume 1", null));
		var chapter = new ChapterModel { VolumeId = volume1.Id, Order = 1, Title = "Chapter 1" };

		await fixture.Chapters.Create(chapter);
		await fixture.Service.CreateNode(chapter.Id, BookNodeType.Chapter, volume1.Id, chapter.Title, chapter.Order);

		var tree = await fixture.Service.GetTree(series.Id);

		Assert.Equal("bookshelf:default", tree.Root.Id);
		Assert.Equal(BookTreeNodeType.BookShelf, tree.Root.Type);
		var seriesNode = Assert.Single(tree.Root.Children);
		Assert.Equal(PrefixedId.ToString(EntityPrefix.Series, series.Id), seriesNode.Id);
		Assert.Equal(["Volume 1", "Volume 2"], seriesNode.Children.Select(n => n.Title));
		var volumeNode = seriesNode.Children[0];
		Assert.Equal(PrefixedId.ToString(EntityPrefix.Volume, volume1.Id), volumeNode.Id);
		Assert.Equal(PrefixedId.ToString(EntityPrefix.Series, series.Id), volumeNode.ParentId);
		var chapterNode = Assert.Single(volumeNode.Children);
		Assert.Equal(PrefixedId.ToString(EntityPrefix.Chapter, chapter.Id), chapterNode.Id);
		Assert.Equal(PrefixedId.ToString(EntityPrefix.Volume, volume1.Id), chapterNode.ParentId);
		Assert.Empty(seriesNode.Children[1].Children);
		Assert.Equal(volume2.Id, fixture.Volumes.Items.Single(v => v.Title == "Volume 2").Id);
	}

	[Fact]
	public async Task CreateNode_RejectsInvalidParentShapes() {
		var fixture = Fixture.Create();
		var series = await fixture.Service.CreateSeries(new CreateSeriesRequestDto("Series", new SeriesMetadataDto()));
		var volume = await fixture.Service.CreateVolume(new CreateVolumeRequestDto(PrefixedId.ToString(EntityPrefix.Series, series.Id), 1, "Volume", null));

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			fixture.Service.CreateNode(Guid.CreateVersion7(), BookNodeType.Series, series.Id, "Nested Series", 1));
		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			fixture.Service.CreateNode(Guid.CreateVersion7(), BookNodeType.Volume, null, "Root Volume", 1));
		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			fixture.Service.CreateNode(Guid.CreateVersion7(), BookNodeType.Volume, volume.Id, "Volume Under Volume", 1));
		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			fixture.Service.CreateNode(Guid.CreateVersion7(), BookNodeType.Chapter, series.Id, "Chapter Under Series", 1));
	}

	[Fact]
	public async Task MoveNode_RejectsDuplicateSiblingOrder() {
		var fixture = Fixture.Create();
		var series = await fixture.Service.CreateSeries(new CreateSeriesRequestDto("Series", new SeriesMetadataDto()));
		var volume1 = await fixture.Service.CreateVolume(new CreateVolumeRequestDto(PrefixedId.ToString(EntityPrefix.Series, series.Id), 1, "Volume 1", null));
		_ = await fixture.Service.CreateVolume(new CreateVolumeRequestDto(PrefixedId.ToString(EntityPrefix.Series, series.Id), 2, "Volume 2", null));

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			fixture.Service.MoveNode(volume1.Id, series.Id, 2));
	}

	[Fact]
	public async Task MoveNode_MirrorsLegacyVolumeAndChapterParentOrder() {
		var fixture = Fixture.Create();
		var series1 = await fixture.Service.CreateSeries(new CreateSeriesRequestDto("Series 1", new SeriesMetadataDto()));
		var series2 = await fixture.Service.CreateSeries(new CreateSeriesRequestDto("Series 2", new SeriesMetadataDto()));
		var volume = await fixture.Service.CreateVolume(new CreateVolumeRequestDto(PrefixedId.ToString(EntityPrefix.Series, series1.Id), 1, "Volume", null));
		var chapter = new ChapterModel { VolumeId = volume.Id, Order = 1, Title = "Chapter" };
		await fixture.Chapters.Create(chapter);
		await fixture.Service.CreateNode(chapter.Id, BookNodeType.Chapter, volume.Id, chapter.Title, chapter.Order);
		var targetVolume = await fixture.Service.CreateVolume(new CreateVolumeRequestDto(PrefixedId.ToString(EntityPrefix.Series, series2.Id), 1, "Target", null));

		await fixture.Service.MoveNode(volume.Id, series2.Id, 2);
		await fixture.Service.MoveNode(chapter.Id, targetVolume.Id, 3);

		Assert.Equal(series2.Id, fixture.Volumes.Items.Single(v => v.Id == volume.Id).SeriesId);
		Assert.Equal(2, fixture.Volumes.Items.Single(v => v.Id == volume.Id).Order);
		Assert.Equal(targetVolume.Id, fixture.Chapters.Items.Single(c => c.Id == chapter.Id).VolumeId);
		Assert.Equal(3, fixture.Chapters.Items.Single(c => c.Id == chapter.Id).Order);
	}

	[Fact]
	public async Task DeleteSubtree_RemovesNodeAndPayloadDescendants() {
		var fixture = Fixture.Create();
		var series = await fixture.Service.CreateSeries(new CreateSeriesRequestDto("Series", new SeriesMetadataDto()));
		var volume = await fixture.Service.CreateVolume(new CreateVolumeRequestDto(PrefixedId.ToString(EntityPrefix.Series, series.Id), 1, "Volume", null));
		var chapter = new ChapterModel { VolumeId = volume.Id, Order = 1, Title = "Chapter" };
		await fixture.Chapters.Create(chapter);
		await fixture.Service.CreateNode(chapter.Id, BookNodeType.Chapter, volume.Id, chapter.Title, chapter.Order);

		var deleted = await fixture.Service.DeleteSubtree(volume.Id);

		Assert.Equal(2, deleted);
		Assert.DoesNotContain(fixture.Tree.Items, n => n.Id == volume.Id || n.Id == chapter.Id);
		Assert.DoesNotContain(fixture.Volumes.Items, v => v.Id == volume.Id);
		Assert.DoesNotContain(fixture.Chapters.Items, c => c.Id == chapter.Id);
		Assert.Contains(fixture.Series.Items, s => s.Id == series.Id);
	}

	[Fact]
	public async Task AssetOwnership_ReconcilesVolumeAndChapterUsageToVolumeOwner() {
		var fixture = Fixture.Create();
		var assetRepository = new InMemoryAssetRepository();
		var ownership = fixture.CreateAssetOwnershipService(assetRepository);
		var series = await fixture.Service.CreateSeries(new CreateSeriesRequestDto("Series", new SeriesMetadataDto()));
		var asset = await assetRepository.Create(CreateAsset(series.Id));
		var assetKey = PrefixedId.ToString(EntityPrefix.Asset, asset.Id);
		var volume = await fixture.Service.CreateVolume(new CreateVolumeRequestDto(
			PrefixedId.ToString(EntityPrefix.Series, series.Id),
			1,
			"Volume",
			new VolumeMetadataDto { CoverImage = assetKey }));
		var chapter = new ChapterModel {
			VolumeId = volume.Id,
			Order = 1,
			Title = "Chapter",
			ContentData = new ChapterContentModel {
				Segments = [new ImageSegmentModel { AssetKey = assetKey }]
			}
		};
		await fixture.Chapters.Create(chapter);
		await fixture.Service.CreateNode(chapter.Id, BookNodeType.Chapter, volume.Id, chapter.Title, chapter.Order);

		await ownership.ReconcileSeriesAsync(series.Id);

		Assert.Equal(volume.Id, assetRepository.Items.Single().OwnerNodeId);
	}

	[Fact]
	public async Task AssetOwnership_ReconcilesSiblingVolumeUsageToSeriesOwner() {
		var fixture = Fixture.Create();
		var assetRepository = new InMemoryAssetRepository();
		var ownership = fixture.CreateAssetOwnershipService(assetRepository);
		var series = await fixture.Service.CreateSeries(new CreateSeriesRequestDto("Series", new SeriesMetadataDto()));
		var asset = await assetRepository.Create(CreateAsset(series.Id));
		var assetKey = PrefixedId.ToString(EntityPrefix.Asset, asset.Id);

		await fixture.Service.CreateVolume(new CreateVolumeRequestDto(
			PrefixedId.ToString(EntityPrefix.Series, series.Id),
			1,
			"Volume 1",
			new VolumeMetadataDto { CoverImage = assetKey }));
		await fixture.Service.CreateVolume(new CreateVolumeRequestDto(
			PrefixedId.ToString(EntityPrefix.Series, series.Id),
			2,
			"Volume 2",
			new VolumeMetadataDto { CoverImage = assetKey }));

		await ownership.ReconcileSeriesAsync(series.Id);

		Assert.Equal(series.Id, assetRepository.Items.Single().OwnerNodeId);
	}

	[Fact]
	public async Task AssetOwnership_ReconcilesCrossSeriesUsageToLogicalShelfOwner() {
		var fixture = Fixture.Create();
		var assetRepository = new InMemoryAssetRepository();
		var ownership = fixture.CreateAssetOwnershipService(assetRepository);
		var otherSeriesId = Guid.CreateVersion7();
		var series = await fixture.Service.CreateSeries(new CreateSeriesRequestDto("Series", new SeriesMetadataDto()));
		var asset = await assetRepository.Create(CreateAsset(series.Id, otherSeriesId));
		var assetKey = PrefixedId.ToString(EntityPrefix.Asset, asset.Id);

		await fixture.Service.UpdateSeries(series.Id, new UpdateSeriesRequestDto(null, new SeriesMetadataDto { CoverImage = assetKey }));

		await ownership.ReconcileSeriesAsync(series.Id);

		Assert.Null(assetRepository.Items.Single().OwnerNodeId);
	}

	[Fact]
	public async Task UpdateSeries_MetadataPartialUpdate_PreservesExistingFields() {
		var fixture = Fixture.Create();
		var series = await fixture.Service.CreateSeries(new CreateSeriesRequestDto("Series", new SeriesMetadataDto {
			Authors = ["Author 1"],
			Artists = ["Artist 1"],
			Tags = ["Tag 1"],
			CoverImage = "old_cover.png"
		}));

		await fixture.Service.UpdateSeries(series.Id, new UpdateSeriesRequestDto(null, new SeriesMetadataDto {
			CoverImage = "new_cover.png"
		}));

		var updated = await fixture.Series.FindOne(series.Id);
		Assert.NotNull(updated);
		Assert.Equal("new_cover.png", updated.Metadata.CoverImage);
		Assert.Equal(["Author 1"], updated.Metadata.Authors);
		Assert.Equal(["Artist 1"], updated.Metadata.Artists);
		Assert.Equal(["Tag 1"], updated.Metadata.Tags);
	}

	[Fact]
	public async Task UpdateVolume_MetadataPartialUpdate_PreservesExistingFields() {
		var fixture = Fixture.Create();
		var series = await fixture.Service.CreateSeries(new CreateSeriesRequestDto("Series", new SeriesMetadataDto()));
		var volume = await fixture.Service.CreateVolume(new CreateVolumeRequestDto(
			PrefixedId.ToString(EntityPrefix.Series, series.Id),
			1,
			"Volume",
			new VolumeMetadataDto {
				CoverImage = "old_cover.png",
				PublicationDate = new DateTime(2020, 1, 1),
				Isbn = "12345"
			}));

		await fixture.Service.UpdateVolume(volume.Id, new UpdateVolumeRequestDto(null, null, new VolumeMetadataDto {
			Isbn = "67890"
		}));

		var updated = await fixture.Volumes.FindOne(volume.Id);
		Assert.NotNull(updated);
		Assert.NotNull(updated.Metadata);
		Assert.Equal("67890", updated.Metadata.Isbn);
		Assert.Equal("old_cover.png", updated.Metadata.CoverImage);
		Assert.Equal(new DateTime(2020, 1, 1), updated.Metadata.PublicationDate);
	}

	private sealed record Fixture(
		BookTreeService Service,
		InMemoryBookTreeRepository Tree,
		InMemorySeriesRepository Series,
		InMemoryVolumeRepository Volumes,
		InMemoryChapterRepository Chapters) {
		public static Fixture Create() {
			var tree = new InMemoryBookTreeRepository();
			var series = new InMemorySeriesRepository();
			var volumes = new InMemoryVolumeRepository();
			var chapters = new InMemoryChapterRepository();
			var mapper = new FakeBookMapper();
			return new Fixture(new BookTreeService(tree, series, volumes, chapters, mapper), tree, series, volumes, chapters);
		}

		public AssetOwnershipService CreateAssetOwnershipService(InMemoryAssetRepository assets) =>
			new(Tree, Series, Volumes, Chapters, assets);
	}

	private static AssetModel CreateAsset(Guid seriesId, Guid? ownerNodeId = null) =>
		new() {
			Id = Guid.CreateVersion7(),
			SeriesId = seriesId,
			OwnerNodeId = ownerNodeId ?? seriesId,
			Path = "asset.png",
			FileHash = Guid.CreateVersion7().ToString("N"),
			RefType = AssetRefType.Content,
			ContentType = "image/png",
			OriginalFileName = "asset.png"
		};

	private sealed class FakeBookMapper : IBookMapper {
		public SeriesModel CreateSeries(CreateSeriesRequestDto dto) => new() {
			Title = dto.Title,
			Metadata = new SeriesMetadata {
				Authors = dto.Metadata.Authors ?? [],
				Artists = dto.Metadata.Artists ?? [],
				Tags = dto.Metadata.Tags ?? [],
				CoverImage = dto.Metadata.CoverImage ?? string.Empty
			}
		};

		public VolumeModel CreateVolume(CreateVolumeRequestDto dto, Guid seriesId) => new() {
			SeriesId = seriesId,
			Order = dto.Order,
			Title = dto.Title,
			Metadata = dto.Metadata is null
				? null
				: new VolumeMetadata {
					CoverImage = dto.Metadata.CoverImage ?? string.Empty,
					PublicationDate = dto.Metadata.PublicationDate,
					Isbn = dto.Metadata.Isbn ?? string.Empty
				}
		};

		public void UpdateSeries(UpdateSeriesRequestDto dto, SeriesModel model) {
			if (dto.Title is not null) model.Title = dto.Title;
			if (dto.Metadata is not null) {
				model.Metadata = new SeriesMetadata {
					Authors = dto.Metadata.Authors ?? model.Metadata.Authors,
					Artists = dto.Metadata.Artists ?? model.Metadata.Artists,
					Tags = dto.Metadata.Tags ?? model.Metadata.Tags,
					CoverImage = dto.Metadata.CoverImage ?? model.Metadata.CoverImage
				};
			}
		}

		public void UpdateVolume(UpdateVolumeRequestDto dto, VolumeModel model) {
			if (dto.Title is not null) model.Title = dto.Title;
			if (dto.Order is not null) model.Order = dto.Order.Value;
			if (dto.Metadata is not null) {
				var current = model.Metadata ?? new VolumeMetadata();
				model.Metadata = new VolumeMetadata {
					CoverImage = dto.Metadata.CoverImage ?? current.CoverImage,
					PublicationDate = dto.Metadata.PublicationDate ?? current.PublicationDate,
					Isbn = dto.Metadata.Isbn ?? current.Isbn
				};
			}
		}

		public ChapterModel CreateChapter(CreateChapterRequestDto dto, Guid volumeId) =>
			new() { VolumeId = volumeId, Order = dto.Order, Title = dto.Title };

		public void UpdateChapter(UpdateChapterRequestDto dto, ChapterModel model) {
			if (dto.Title is not null) model.Title = dto.Title;
			if (dto.Order is not null) model.Order = dto.Order.Value;
		}

		public void MergeChapter(ChapterModel source, ChapterContentModel sourceContent, ChapterModel target) {
			target.Title = source.Title;
			target.Status = source.Status;
			target.ContentData = sourceContent;
		}

		public ChapterResponseDto ToChapterDto(ChapterModel model) => throw new NotSupportedException();
		public ChapterListResponseDto ToChapterListDto(ChapterModel model) => throw new NotSupportedException();
		public SeriesResponseDto ToSeriesDto(SeriesModel model) => throw new NotSupportedException();
		public VolumeResponseDto ToVolumeDto(VolumeModel model) => throw new NotSupportedException();
		public AssetResponseDto ToAssetDto(AssetModel model) => throw new NotSupportedException();
		public TextSegmentDto ToTextSegmentDto(TextSegmentModel model) => throw new NotSupportedException();
	}

	private abstract class InMemoryRepository<T> : IRepository<T> where T : BaseModel {
		public List<T> Items { get; } = [];

		public virtual Task<T?> FindOne(Guid id, CancellationToken cancellationToken = default) =>
			Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

		public virtual Task<T?> FindOneTracked(Guid id, CancellationToken cancellationToken = default) =>
			FindOne(id, cancellationToken);

		public Task<PagedResult<T>> FindAll(int pageIndex, int pageSize, CancellationToken cancellationToken = default) {
			var items = Items.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
			return Task.FromResult(new PagedResult<T>(items, Items.Count, pageIndex, pageSize));
		}

		public virtual Task<T> Create(T entity, CancellationToken cancellationToken = default) {
			Items.Add(entity);
			return Task.FromResult(entity);
		}

		public Task<IEnumerable<T>> CreateBulk(IEnumerable<T> entities, CancellationToken cancellationToken = default) {
			var list = entities.ToList();
			Items.AddRange(list);
			return Task.FromResult<IEnumerable<T>>(list);
		}

		public virtual Task<T> Update(T entity, CancellationToken cancellationToken = default) {
			var index = Items.FindIndex(i => i.Id == entity.Id);
			if (index >= 0) {
				Items[index] = entity;
			}
			return Task.FromResult(entity);
		}

		public Task<IEnumerable<T>> UpdateBulk(IEnumerable<T> entities, CancellationToken cancellationToken = default) {
			var list = entities.ToList();
			foreach (var entity in list) {
				var index = Items.FindIndex(i => i.Id == entity.Id);
				if (index >= 0) {
					Items[index] = entity;
				}
			}
			return Task.FromResult<IEnumerable<T>>(list);
		}

		public virtual Task<int> Delete(Guid id, CancellationToken cancellationToken = default) {
			return Task.FromResult(Items.RemoveAll(i => i.Id == id));
		}
	}

	private sealed class InMemoryBookTreeRepository : InMemoryRepository<BookNodeModel>, IBookTreeRepository {
		public Task<IEnumerable<BookNodeModel>> FindChildren(Guid? parentId, CancellationToken cancellationToken = default) =>
			Task.FromResult<IEnumerable<BookNodeModel>>(Items.Where(n => n.ParentId == parentId).OrderBy(n => n.Order).ToList());

		public Task<IEnumerable<BookNodeModel>> FindChildren(Guid? parentId, int pageIndex, int pageSize, CancellationToken cancellationToken = default) =>
			Task.FromResult<IEnumerable<BookNodeModel>>(Items
				.Where(n => n.ParentId == parentId)
				.OrderBy(n => n.Order)
				.Skip((pageIndex - 1) * pageSize)
				.Take(pageSize)
				.ToList());

		public Task<int> CountChildren(Guid? parentId, CancellationToken cancellationToken = default) =>
			Task.FromResult(Items.Count(n => n.ParentId == parentId));

		public Task<BookNodeModel?> FindChildByOrder(Guid? parentId, float order, CancellationToken cancellationToken = default) =>
			Task.FromResult(Items.FirstOrDefault(n => n.ParentId == parentId && n.Order == order));

		public Task<IReadOnlyList<BookNodeModel>> FindSeriesTree(Guid seriesId, CancellationToken cancellationToken = default) {
			var series = Items.Where(n => n.Id == seriesId).ToList();
			var volumes = Items.Where(n => n.ParentId == seriesId).OrderBy(n => n.Order).ToList();
			var volumeIds = volumes.Select(v => v.Id).ToHashSet();
			var chapters = Items.Where(n => n.ParentId is not null && volumeIds.Contains(n.ParentId.Value)).OrderBy(n => n.Order).ToList();
			return Task.FromResult<IReadOnlyList<BookNodeModel>>([.. series, .. volumes, .. chapters]);
		}

		public Task<IReadOnlyList<BookNodeModel>> FindSubtree(Guid nodeId, CancellationToken cancellationToken = default) {
			var result = new List<BookNodeModel>();
			var pending = new Queue<Guid>();
			pending.Enqueue(nodeId);
			while (pending.Count > 0) {
				var id = pending.Dequeue();
				var node = Items.FirstOrDefault(n => n.Id == id);
				if (node is null) continue;
				result.Add(node);
				foreach (var child in Items.Where(n => n.ParentId == id)) {
					pending.Enqueue(child.Id);
				}
			}
			return Task.FromResult<IReadOnlyList<BookNodeModel>>(result);
		}

		public Task DeleteMany(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) {
			var set = ids.ToHashSet();
			Items.RemoveAll(n => set.Contains(n.Id));
			return Task.CompletedTask;
		}
	}

	private sealed class InMemorySeriesRepository : InMemoryRepository<SeriesModel>, ISeriesRepository {
		public Task<SeriesModel?> FindOneByTitle(string title, CancellationToken cancellationToken = default) =>
			Task.FromResult(Items.FirstOrDefault(s => s.Title == title));
	}

	private sealed class InMemoryVolumeRepository : InMemoryRepository<VolumeModel>, IVolumeRepository {
		public Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, CancellationToken cancellationToken = default) =>
			Task.FromResult<IEnumerable<VolumeModel>>(Items.Where(v => v.SeriesId == seriesId).OrderBy(v => v.Order).ToList());

		public Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, int pageIndex, int pageSize, CancellationToken cancellationToken = default) =>
			Task.FromResult<IEnumerable<VolumeModel>>(Items.Where(v => v.SeriesId == seriesId).OrderBy(v => v.Order).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList());

		public Task<int> CountBySeriesId(Guid seriesId, CancellationToken cancellationToken = default) =>
			Task.FromResult(Items.Count(v => v.SeriesId == seriesId));

		public Task<VolumeModel?> FindBySeriesIdAndOrder(Guid seriesId, float order, CancellationToken cancellationToken = default) =>
			Task.FromResult(Items.FirstOrDefault(v => v.SeriesId == seriesId && v.Order == order));
	}

	private sealed class InMemoryChapterRepository : InMemoryRepository<ChapterModel>, IChapterRepository {
		public Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId, CancellationToken cancellationToken = default) =>
			Task.FromResult<IEnumerable<ChapterModel>>(Items.Where(c => c.VolumeId == volumeId).OrderBy(c => c.Order).ToList());

		public Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId, int pageIndex, int pageSize, CancellationToken cancellationToken = default) =>
			Task.FromResult<IEnumerable<ChapterModel>>(Items.Where(c => c.VolumeId == volumeId).OrderBy(c => c.Order).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList());

		public Task<int> CountByVolumeId(Guid volumeId, CancellationToken cancellationToken = default) =>
			Task.FromResult(Items.Count(c => c.VolumeId == volumeId));

		public Task<IEnumerable<ChapterModel>> FindByVolumeIds(IEnumerable<Guid> volumeIds, CancellationToken cancellationToken = default) {
			var set = volumeIds.ToHashSet();
			return Task.FromResult<IEnumerable<ChapterModel>>(Items.Where(c => set.Contains(c.VolumeId)).OrderBy(c => c.VolumeId).ThenBy(c => c.Order).ToList());
		}

		public Task<IEnumerable<ChapterModel>> FindByVolumeIdsWithContent(IEnumerable<Guid> volumeIds, CancellationToken cancellationToken = default) =>
			FindByVolumeIds(volumeIds, cancellationToken);

		public Task<ChapterModel?> FindByVolumeIdAndOrder(Guid volumeId, float order, CancellationToken cancellationToken = default) =>
			Task.FromResult(Items.FirstOrDefault(c => c.VolumeId == volumeId && c.Order == order));
	}

	private sealed class InMemoryAssetRepository : InMemoryRepository<AssetModel>, IAssetRepository {
		public Task<AssetModel?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default) =>
			Task.FromResult(Items.FirstOrDefault(a => a.FileHash == fileHash));

		public Task<AssetModel?> GetBySeriesAndFileHashAsync(Guid seriesId, string fileHash, CancellationToken cancellationToken = default) =>
			Task.FromResult(Items.FirstOrDefault(a => a.SeriesId == seriesId && a.FileHash == fileHash));

		public Task<IReadOnlyDictionary<Guid, AssetModel>> FindByIdsAsync(IEnumerable<Guid> assetIds, CancellationToken cancellationToken = default) {
			var set = assetIds.ToHashSet();
			return Task.FromResult<IReadOnlyDictionary<Guid, AssetModel>>(Items.Where(a => set.Contains(a.Id)).ToDictionary(a => a.Id));
		}
	}
}
