namespace Grimoire.Tests.Application;

using System;
using System.Linq;
using System.Threading.Tasks;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Dto.Book.Segment;
using Grimoire.Application.Mapper;
using Grimoire.Application.Service.Strategy;
using Grimoire.Domain.Entity.Book;
using Grimoire.Domain.Entity.Book.Segment;
using Grimoire.Tests.TestInfrastructure;
using Xunit;

public class IngestionStrategyTests {
	private static readonly BookMapper Mapper = new();

	// ── PreProcessedIngestionStrategy Tests ───────────────────────────────────

	[Fact]
	public void PreProcessedIngestionStrategy_CanHandle_ValidContent_ReturnsTrue() {
		var strategy = new PreProcessedIngestionStrategy(Mapper);
		var dto = new CreateChapterRequestDto(
			VolumeId: Guid.NewGuid().ToString(),
			Order: 1,
			Title: "Test",
			Content: [],
			Footnotes: [],
			RawContent: null
		);

		Assert.True(strategy.CanHandle(dto));
	}

	[Fact]
	public void PreProcessedIngestionStrategy_CanHandle_NullContent_ReturnsFalse() {
		var strategy = new PreProcessedIngestionStrategy(Mapper);
		var dto = new CreateChapterRequestDto(
			VolumeId: Guid.NewGuid().ToString(),
			Order: 1,
			Title: "Test",
			Content: null,
			Footnotes: [],
			RawContent: "some markdown"
		);

		Assert.False(strategy.CanHandle(dto));
	}

	[Fact]
	public async Task PreProcessedIngestionStrategy_ExecuteAsync_ReturnsResult() {
		var strategy = new PreProcessedIngestionStrategy(Mapper);
		var volumeId = Guid.NewGuid();
		var originalFootnoteId = Guid.NewGuid().ToString();

		var segment = new TextSegmentDto {
			Id = Guid.NewGuid().ToString(),
			Runs = [new TextRunDto("text", FootnoteId: originalFootnoteId)]
		};
		var footnote = new ImportFootnoteDto { InitialId = originalFootnoteId };

		var dto = new CreateChapterRequestDto(
			VolumeId: volumeId.ToString(),
			Order: 2,
			Title: "Chapter 1",
			Content: [segment],
			Footnotes: [footnote],
			RawContent: null
		);

		var result = await strategy.ExecuteAsync(dto, volumeId);

		Assert.NotNull(result);
		Assert.Equal(2, result.Chapter.Order);
		Assert.Equal("Chapter 1", result.Chapter.Title);
		Assert.Equal(ChapterStatus.Done, result.Chapter.Status);
		Assert.Null(result.Source);

		var mappedSegment = Assert.IsType<TextSegmentModel>(Assert.Single(result.Segments.OfType<TextSegmentModel>()));
		var mappedFootnoteId = mappedSegment.Runs.First().FootnoteId;
		Assert.NotNull(mappedFootnoteId);
		Assert.NotEqual(originalFootnoteId, mappedFootnoteId);

		var systemFootnote = Assert.Single(result.Segments.OfType<FootnoteSegmentModel>());
		Assert.Equal(mappedFootnoteId, systemFootnote.Id.ToString());
	}

	[Fact]
	public async Task PreProcessedIngestionStrategy_ExecuteAsync_CannotHandle_ThrowsInvalidOperationException() {
		var strategy = new PreProcessedIngestionStrategy(Mapper);
		var dto = new CreateChapterRequestDto(
			VolumeId: Guid.NewGuid().ToString(),
			Order: 1,
			Title: "Test",
			Content: null,
			Footnotes: [],
			RawContent: "some md"
		);

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			strategy.ExecuteAsync(dto, Guid.NewGuid()));
	}

	// ── RawMarkdownIngestionStrategy Tests ────────────────────────────────────

	[Fact]
	public void RawMarkdownIngestionStrategy_CanHandle_ValidRawContent_ReturnsTrue() {
		var repo = new InMemoryVolumeRepository();
		var strategy = new RawMarkdownIngestionStrategy(new MarkdownSegmentParser(), repo);
		var dto = new CreateChapterRequestDto(
			VolumeId: Guid.NewGuid().ToString(),
			Order: 1,
			Title: "Test",
			Content: null,
			Footnotes: [],
			RawContent: "some markdown"
		);

		Assert.True(strategy.CanHandle(dto));
	}

	[Fact]
	public void RawMarkdownIngestionStrategy_CanHandle_NullRawContent_ReturnsFalse() {
		var repo = new InMemoryVolumeRepository();
		var strategy = new RawMarkdownIngestionStrategy(new MarkdownSegmentParser(), repo);
		var dto = new CreateChapterRequestDto(
			VolumeId: Guid.NewGuid().ToString(),
			Order: 1,
			Title: "Test",
			Content: [],
			Footnotes: [],
			RawContent: null
		);

		Assert.False(strategy.CanHandle(dto));
	}

	[Fact]
	public void RawMarkdownIngestionStrategy_CanHandle_ContentContainsHtml_ReturnsTrueAndPreservesVerbatim() {
		var volumeId = Guid.NewGuid();
		var seriesId = Guid.NewGuid();
		var repo = new InMemoryVolumeRepository();
		repo.Create(new VolumeModel {
			Id = volumeId,
			Path = $"n{seriesId:N}.n{volumeId:N}",
			Order = 1,
			Title = "Volume 1"
		}).GetAwaiter().GetResult();

		var strategy = new RawMarkdownIngestionStrategy(new MarkdownSegmentParser(), repo);
		const string raw = "This contains <div>HTML</div>";
		var dto = new CreateChapterRequestDto(
			VolumeId: volumeId.ToString(),
			Order: 1,
			Title: "Test",
			Content: null,
			Footnotes: [],
			RawContent: raw
		);

		Assert.True(strategy.CanHandle(dto));

		var result = strategy.ExecuteAsync(dto, volumeId).GetAwaiter().GetResult();
		var text = Assert.Single(result.Segments.OfType<TextSegmentModel>());
		Assert.Contains("<div>", string.Concat(text.Runs.Select(r => r.Text)));
	}

	[Fact]
	public async Task RawMarkdownIngestionStrategy_ExecuteAsync_ValidRawContent_CreatesTextSegmentsAndSourceMaterial() {
		var volumeId = Guid.NewGuid();
		var seriesId = Guid.NewGuid();
		var repo = new InMemoryVolumeRepository();
		await repo.Create(new VolumeModel {
			Id = volumeId,
			Path = $"n{seriesId:N}.n{volumeId:N}",
			Order = 1,
			Title = "Volume 1"
		});

		var strategy = new RawMarkdownIngestionStrategy(new MarkdownSegmentParser(), repo);
		var dto = new CreateChapterRequestDto(
			VolumeId: volumeId.ToString(),
			Order: 5,
			Title: "Chapter 5",
			Content: null,
			Footnotes: [],
			RawContent: "Line 1\n\nLine 2"
		);

		var result = await strategy.ExecuteAsync(dto, volumeId);

		Assert.NotNull(result);
		Assert.Equal(5, result.Chapter.Order);
		Assert.Equal("Chapter 5", result.Chapter.Title);
		Assert.Equal(ChapterStatus.Draft, result.Chapter.Status);

		Assert.NotNull(result.Source);
		Assert.Equal(seriesId, result.Source.SeriesId);
		Assert.Equal("Chapter 5 - Raw Source", result.Source.Title);
		Assert.Equal("Line 1\n\nLine 2", result.Source.MarkdownContent);

		Assert.Equal(2, result.Segments.Count);
		var seg1 = Assert.IsType<TextSegmentModel>(result.Segments[0]);
		Assert.Equal("Line 1", seg1.Runs.First().Text);
		var seg2 = Assert.IsType<TextSegmentModel>(result.Segments[1]);
		Assert.Equal("Line 2", seg2.Runs.First().Text);
	}

	[Fact]
	public async Task RawMarkdownIngestionStrategy_ExecuteAsync_VolumeNotFound_ThrowsInvalidOperationException() {
		var repo = new InMemoryVolumeRepository();
		var strategy = new RawMarkdownIngestionStrategy(new MarkdownSegmentParser(), repo);
		var dto = new CreateChapterRequestDto(
			VolumeId: Guid.NewGuid().ToString(),
			Order: 1,
			Title: "Test",
			Content: null,
			Footnotes: [],
			RawContent: "some markdown"
		);

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			strategy.ExecuteAsync(dto, Guid.NewGuid()));
	}

	// ── IngestionStrategyFactory Tests ────────────────────────────────────────

	[Fact]
	public void IngestionStrategyFactory_GetStrategy_ReturnsFirstMatchingStrategy() {
		var strategy1 = new PreProcessedIngestionStrategy(Mapper);
		var repo = new InMemoryVolumeRepository();
		var strategy2 = new RawMarkdownIngestionStrategy(new MarkdownSegmentParser(), repo);

		var factory = new IngestionStrategyFactory([strategy1, strategy2]);

		// PreProcessed dto
		var dtoPre = new CreateChapterRequestDto(
			VolumeId: Guid.NewGuid().ToString(),
			Order: 1,
			Title: "Test",
			Content: [],
			Footnotes: [],
			RawContent: null
		);

		var resolvedPre = factory.GetStrategy(dtoPre);
		Assert.Same(strategy1, resolvedPre);

		// Raw dto
		var dtoRaw = new CreateChapterRequestDto(
			VolumeId: Guid.NewGuid().ToString(),
			Order: 1,
			Title: "Test",
			Content: null,
			Footnotes: [],
			RawContent: "markdown text"
		);

		var resolvedRaw = factory.GetStrategy(dtoRaw);
		Assert.Same(strategy2, resolvedRaw);
	}

	[Fact]
	public void IngestionStrategyFactory_GetStrategy_NoMatchingStrategy_ThrowsInvalidOperationException() {
		var repo = new InMemoryVolumeRepository();
		var strategy = new RawMarkdownIngestionStrategy(new MarkdownSegmentParser(), repo);
		var factory = new IngestionStrategyFactory([strategy]);

		// Preprocessed DTO cannot be handled by Raw strategy
		var dto = new CreateChapterRequestDto(
			VolumeId: Guid.NewGuid().ToString(),
			Order: 1,
			Title: "Test",
			Content: null,
			Footnotes: [],
			RawContent: null
		);

		Assert.Throws<InvalidOperationException>(() => factory.GetStrategy(dto));
	}
}
