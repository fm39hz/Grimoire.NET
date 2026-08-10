namespace Grimoire.Tests.Domain;

using Grimoire.Domain.Entity.Book;
using Xunit;

public sealed class SeriesRevisionTests {
	[Fact]
	public void AdvanceRevision_RequiresExpectedCurrentRevision() {
		var series = new SeriesModel { Title = "Book", Path = "n11111111111111111111111111111111" };

		series.AdvanceRevision(0);

		Assert.Equal(1, series.Revision);
		Assert.Throws<InvalidOperationException>(() => series.AdvanceRevision(0));
	}
}
