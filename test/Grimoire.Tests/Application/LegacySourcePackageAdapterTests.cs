namespace Grimoire.Tests.Application;

using Grimoire.Application.Dto.Book;
using Grimoire.Application.Ingestion.Legacy;
using Xunit;

public sealed class LegacySourcePackageAdapterTests {
	[Fact]
	public void SeriesSyncConversionIsDeterministicAndPatchOnly() {
		var request = new SyncSeriesRequestDto([
			new SyncVolumeDto(1, "Volume 1", null, [new SyncChapterDto(1, "Chapter 1", null, null, "text")])
		]);
		var adapter = new LegacySourcePackageAdapter();
		var seriesId = Guid.Parse("11111111-1111-1111-1111-111111111111");

		var first = adapter.FromSeriesSync(seriesId, request);
		var second = adapter.FromSeriesSync(seriesId, request);

		Assert.Equal(first.IdempotencyKey, second.IdempotencyKey);
		Assert.Equal(Grimoire.Domain.Entity.Ingestion.ImportSemantics.Patch, first.Semantics);
		Assert.Equal("ser_11111111-1111-1111-1111-111111111111", first.TargetHint?.SeriesId);
		Assert.NotNull(first.Nodes[0].Children?[0].Content?.ContentHash);
	}
}
