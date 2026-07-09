namespace Grimoire.Tests.Application;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Dto.Book.Segment;
using Grimoire.Application.Dto.Book.Metadata;
using Grimoire.Application.Dto.Book.Tree;
using Grimoire.Application.Mapper;
using Grimoire.Application.Service.Implementation;
using Grimoire.Domain.Common;
using Grimoire.Domain.Common.Repository;
using Grimoire.Domain.Entity.Book;
using Grimoire.Domain.Entity.Book.Metadata;
using Grimoire.Domain.Entity.Book.Segment;
using Grimoire.Tests.TestInfrastructure;
using Xunit;

public sealed class BookTreeServiceTests {

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

		public VolumeModel CreateVolume(CreateVolumeRequestDto dto) => new() {
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
		}

		public void UpdateVolume(UpdateVolumeRequestDto dto, VolumeModel model) {
			if (dto.Title is not null) model.Title = dto.Title;
			if (dto.Order is not null) model.Order = dto.Order.Value;
		}

		public ChapterModel CreateChapter(CreateChapterRequestDto dto) =>
			new() { Order = dto.Order, Title = dto.Title };

		public void UpdateChapter(UpdateChapterRequestDto dto, ChapterModel model) {
			if (dto.Title is not null) model.Title = dto.Title;
		}

		public ChapterResponseDto ToChapterDto(ChapterModel model) => throw new NotSupportedException();
		public ChapterResponseDto ToChapterDto(ChapterModel model, IEnumerable<SegmentModel> segments) => throw new NotSupportedException();
		public ChapterListResponseDto ToChapterListDto(ChapterModel model) => throw new NotSupportedException();
		public SeriesResponseDto ToSeriesDto(SeriesModel model) => throw new NotSupportedException();
		public VolumeResponseDto ToVolumeDto(VolumeModel model) => throw new NotSupportedException();
		public AssetResponseDto ToAssetDto(AssetModel model) => throw new NotSupportedException();
		public TextSegmentDto ToTextSegmentDto(TextSegmentModel model) => throw new NotSupportedException();
		public IQueryable<VolumeResponseDto> ProjectToVolumeDto(IQueryable<VolumeModel> query) => throw new NotSupportedException();
		public IQueryable<ChapterListResponseDto> ProjectToChapterListDto(IQueryable<ChapterModel> query) => throw new NotSupportedException();
	}

	private sealed record Fixture(
		BookTreeService Service,
		InMemorySeriesRepository Series,
		InMemoryVolumeRepository Volumes,
		InMemoryChapterRepository Chapters) {
		public static Fixture Create() {
			var chapters = new InMemoryChapterRepository();
			var volumes = new InMemoryVolumeRepository(chapters);
			var series = new InMemorySeriesRepository(volumes, chapters);
			var mapper = new FakeBookMapper();
			return new Fixture(new BookTreeService(series, volumes, chapters, new NoOpUnitOfWork(), mapper), series, volumes, chapters);
		}
	}

	[Fact]
	public async Task GetTree_ReturnsShelfRootWithOrderedSeriesVolumeChapterHierarchy() {
		var fixture = Fixture.Create();
		var series = await fixture.Service.CreateSeries(new CreateSeriesRequestDto("Series", new SeriesMetadataDto()));
		var volume2 = await fixture.Service.CreateVolume(new CreateVolumeRequestDto(PrefixedId.ToString(EntityPrefix.Series, series.Id), 2, "Volume 2", null));
		var volume1 = await fixture.Service.CreateVolume(new CreateVolumeRequestDto(PrefixedId.ToString(EntityPrefix.Series, series.Id), 1, "Volume 1", null));

		var chapter = new ChapterModel {
			Id = Guid.CreateVersion7(),
			Path = $"{volume1.Path}.nc1",
			Order = 1,
			Title = "Chapter 1"
		};
		await fixture.Chapters.Create(chapter);

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
	}

	[Fact]
	public async Task MoveNode_UpdatesPathsCorrectly() {
		var fixture = Fixture.Create();
		var series1 = await fixture.Service.CreateSeries(new CreateSeriesRequestDto("Series 1", new SeriesMetadataDto()));
		var series2 = await fixture.Service.CreateSeries(new CreateSeriesRequestDto("Series 2", new SeriesMetadataDto()));
		
		var volume = await fixture.Service.CreateVolume(new CreateVolumeRequestDto(PrefixedId.ToString(EntityPrefix.Series, series1.Id), 1, "Volume", null));

		await fixture.Service.MoveNode(volume.Id, series2.Id, 2);

		var updatedVol = await fixture.Volumes.FindOne(volume.Id);
		Assert.NotNull(updatedVol);
		Assert.Equal(2, updatedVol.Order);
		Assert.Equal($"{series2.Path}.n{volume.Id:N}", updatedVol.Path);
	}

	[Fact]
	public async Task DeleteSubtree_DeletesCorrectNode() {
		var fixture = Fixture.Create();
		var series = await fixture.Service.CreateSeries(new CreateSeriesRequestDto("Series", new SeriesMetadataDto()));
		var volume = await fixture.Service.CreateVolume(new CreateVolumeRequestDto(PrefixedId.ToString(EntityPrefix.Series, series.Id), 1, "Volume", null));

		var count = await fixture.Service.DeleteSubtree(volume.Id);
		Assert.Equal(1, count);

		var deletedVol = await fixture.Volumes.FindOne(volume.Id);
		Assert.Null(deletedVol);
	}
}
