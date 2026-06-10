namespace Grimoire.Tests.Application;

using System.Linq;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Service.Strategy;
using Grimoire.Domain.Entity.Book;
using Grimoire.Domain.Entity.Book.Segment;
using Grimoire.Tests.TestInfrastructure;
using Xunit;

public sealed class IngestionStrategyTests {
	// ── PreProcessedIngestionStrategy Tests ───────────────────────────────────

	[Fact]
	public void PreProcessedIngestionStrategy_CanHandle_ContentIsNotNull_ReturnsTrue() {
		var strategy = new PreProcessedIngestionStrategy();
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
	public void PreProcessedIngestionStrategy_CanHandle_ContentIsNull_ReturnsFalse() {
		var strategy = new PreProcessedIngestionStrategy();
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
	public async Task PreProcessedIngestionStrategy_ExecuteAsync_RemapsFootnotesAndReturnsResult() {
		var strategy = new PreProcessedIngestionStrategy();
		var volumeId = Guid.NewGuid();
		var originalFootnoteId = "fn-original";

		var segment = new TextSegmentModel {
			Id = Guid.NewGuid(),
			Runs = [new TextRun("text") { FootnoteId = originalFootnoteId }]
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
		Assert.Equal(volumeId, result.Chapter.VolumeId);
		Assert.Equal(2, result.Chapter.Order);
		Assert.Equal("Chapter 1", result.Chapter.Title);
		Assert.Equal(ChapterStatus.Done, result.Chapter.Status);
		Assert.Null(result.Source);

		var mappedSegment = Assert.IsType<TextSegmentModel>(Assert.Single(result.Content.Segments));
		var mappedFootnoteId = mappedSegment.Runs.First().FootnoteId;
		Assert.NotNull(mappedFootnoteId);
		Assert.NotEqual(originalFootnoteId, mappedFootnoteId);

		var systemFootnote = Assert.Single(result.Content.Footnotes);
		Assert.Equal(mappedFootnoteId, systemFootnote.Id.ToString());
	}

	[Fact]
	public async Task PreProcessedIngestionStrategy_ExecuteAsync_CannotHandle_ThrowsInvalidOperationException() {
		var strategy = new PreProcessedIngestionStrategy();
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
		var strategy = new RawMarkdownIngestionStrategy(repo);
		var dto = new CreateChapterRequestDto(
			VolumeId: Guid.NewGuid().ToString(),
			Order: 1,
			Title: "Test",
			Content: null,
			Footnotes: [],
			RawContent: "This is raw markdown content.\nAnother line."
		);

		Assert.True(strategy.CanHandle(dto));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void RawMarkdownIngestionStrategy_CanHandle_NullOrWhitespaceContent_ReturnsFalse(string? rawContent) {
		var repo = new InMemoryVolumeRepository();
		var strategy = new RawMarkdownIngestionStrategy(repo);
		var dto = new CreateChapterRequestDto(
			VolumeId: Guid.NewGuid().ToString(),
			Order: 1,
			Title: "Test",
			Content: null,
			Footnotes: [],
			RawContent: rawContent
		);

		Assert.False(strategy.CanHandle(dto));
	}

	[Fact]
	public void RawMarkdownIngestionStrategy_CanHandle_ContainsHtml_ReturnsFalse() {
		var repo = new InMemoryVolumeRepository();
		var strategy = new RawMarkdownIngestionStrategy(repo);
		var dto = new CreateChapterRequestDto(
			VolumeId: Guid.NewGuid().ToString(),
			Order: 1,
			Title: "Test",
			Content: null,
			Footnotes: [],
			RawContent: "This contains <div>HTML</div>"
		);

		Assert.False(strategy.CanHandle(dto));
	}

	[Fact]
	public async Task RawMarkdownIngestionStrategy_ExecuteAsync_ValidRawContent_CreatesTextSegmentsAndSourceMaterial() {
		var volumeId = Guid.NewGuid();
		var seriesId = Guid.NewGuid();
		var repo = new InMemoryVolumeRepository();
		await repo.Create(new VolumeModel {
			Id = volumeId,
			SeriesId = seriesId,
			Order = 1,
			Title = "Volume 1"
		});

		var strategy = new RawMarkdownIngestionStrategy(repo);
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
		Assert.Equal(volumeId, result.Chapter.VolumeId);
		Assert.Equal(5, result.Chapter.Order);
		Assert.Equal("Chapter 5", result.Chapter.Title);
		Assert.Equal(ChapterStatus.Draft, result.Chapter.Status);

		Assert.NotNull(result.Source);
		Assert.Equal(seriesId, result.Source.SeriesId);
		Assert.Equal("Chapter 5 - Raw Source", result.Source.Title);
		Assert.Equal("Line 1\n\nLine 2", result.Source.MarkdownContent);

		Assert.Equal(2, result.Content.Segments.Count);
		var seg1 = Assert.IsType<TextSegmentModel>(result.Content.Segments[0]);
		Assert.Equal("Line 1", seg1.Runs.First().Text);
		var seg2 = Assert.IsType<TextSegmentModel>(result.Content.Segments[1]);
		Assert.Equal("Line 2", seg2.Runs.First().Text);
	}

	[Fact]
	public async Task RawMarkdownIngestionStrategy_ExecuteAsync_VolumeNotFound_ThrowsInvalidOperationException() {
		var repo = new InMemoryVolumeRepository();
		var strategy = new RawMarkdownIngestionStrategy(repo);
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
		var strategy1 = new PreProcessedIngestionStrategy();
		var repo = new InMemoryVolumeRepository();
		var strategy2 = new RawMarkdownIngestionStrategy(repo);

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
		var strategy = new RawMarkdownIngestionStrategy(repo);
		var factory = new IngestionStrategyFactory([strategy]);

		// Preprocessed DTO cannot be handled by Raw strategy
		var dto = new CreateChapterRequestDto(
			VolumeId: Guid.NewGuid().ToString(),
			Order: 1,
			Title: "Test",
			Content: null, // this makes strategy.CanHandle return false because RawContent is also null
			Footnotes: [],
			RawContent: null
		);

		Assert.Throws<InvalidOperationException>(() => factory.GetStrategy(dto));
	}
}
