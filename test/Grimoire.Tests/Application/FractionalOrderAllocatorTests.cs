namespace Grimoire.Tests.Application;

using Grimoire.Application.Ingestion.Reconciliation;
using Xunit;

public sealed class FractionalOrderAllocatorTests {
	[Fact]
	public void InsertedPrologueIsPlacedBeforeTheNextAnchor() {
		var result = FractionalOrderAllocator.Allocate(
			[("chapter-1", 1), ("chapter-2", 2)],
			hint: 1,
			previousTargetId: null,
			nextTargetId: "chapter-1");

		Assert.Equal(0, result);
	}

	[Fact]
	public void InsertionBetweenAnchorsUsesMidpoint() {
		var result = FractionalOrderAllocator.Allocate(
			[("before", 2), ("after", 4)],
			hint: 3,
			previousTargetId: "before",
			nextTargetId: "after");

		Assert.Equal(3, result);
	}

	[Fact]
	public void CollidingUnanchoredHintAppendsWithoutCollision() {
		var result = FractionalOrderAllocator.Allocate(
			[("one", 1), ("two", 2)],
			hint: 1,
			previousTargetId: null,
			nextTargetId: null);

		Assert.Equal(3, result);
	}
}
