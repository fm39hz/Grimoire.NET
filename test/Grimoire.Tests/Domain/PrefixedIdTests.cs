namespace Grimoire.Tests.Domain;

using Grimoire.Domain.Common;
using Xunit;

public sealed class PrefixedIdTests {
	[Fact]
	public void ToString_WithPrefixAndGuid_ProducesCorrectFormat() {
		var id = Guid.Parse("00000000-0000-0000-0000-000000000001");
		var result = PrefixedId.ToString("ser", id);
		Assert.Equal("ser_00000000-0000-0000-0000-000000000001", result);
	}

	[Fact]
	public void ToGuid_WithValidPrefixedId_ExtractsGuid() {
		var id = Guid.CreateVersion7();
		var prefixed = PrefixedId.ToString("vol", id);
		var result = PrefixedId.ToGuid(prefixed);
		Assert.Equal(id, result);
	}

	[Fact]
	public void ToGuid_WithExpectedPrefix_Succeeds() {
		var id = Guid.CreateVersion7();
		var prefixed = PrefixedId.ToString("chp", id);
		var result = PrefixedId.ToGuid(prefixed, "chp");
		Assert.Equal(id, result);
	}

	[Fact]
	public void ToGuid_WithWrongPrefix_ThrowsArgumentException() {
		var prefixed = PrefixedId.ToString("ser", Guid.CreateVersion7());
		Assert.Throws<ArgumentException>(() => PrefixedId.ToGuid(prefixed, "vol"));
	}

	[Fact]
	public void ToGuid_WithMissingGuid_ThrowsFormatException() {
		Assert.Throws<FormatException>(() => PrefixedId.ToGuid("ser_not-a-guid"));
	}

	[Fact]
	public void ToGuid_WithNoSeparator_ThrowsFormatException() {
		Assert.Throws<FormatException>(() => PrefixedId.ToGuid("noguid"));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void ToGuid_WithNullOrEmpty_ThrowsArgumentException(string? input) {
		Assert.Throws<ArgumentException>(() => PrefixedId.ToGuid(input!));
	}

	[Fact]
	public void TryToGuid_WithValidInput_ReturnsTrueAndGuid() {
		var id = Guid.CreateVersion7();
		var prefixed = PrefixedId.ToString("ast", id);
		var success = PrefixedId.TryToGuid(prefixed, out var result);
		Assert.True(success);
		Assert.Equal(id, result);
	}

	[Fact]
	public void TryToGuid_WithInvalidInput_ReturnsFalse() {
		Assert.False(PrefixedId.TryToGuid("not-valid", out _));
	}

	[Fact]
	public void TryToGuid_WithNullInput_ReturnsFalse() {
		Assert.False(PrefixedId.TryToGuid(null, out _));
	}

	[Fact]
	public void TryToGuid_WithCorrectPrefix_ReturnsTrue() {
		var id = Guid.CreateVersion7();
		var prefixed = PrefixedId.ToString("ser", id);
		var success = PrefixedId.TryToGuid(prefixed, "ser", out var result);
		Assert.True(success);
		Assert.Equal(id, result);
	}

	[Fact]
	public void TryToGuid_WithWrongPrefix_ReturnsFalse() {
		var prefixed = PrefixedId.ToString("ser", Guid.CreateVersion7());
		Assert.False(PrefixedId.TryToGuid(prefixed, "vol", out _));
	}

	[Fact]
	public void GetPrefix_WithValidPrefixedId_ReturnsPrefix() {
		var prefixed = PrefixedId.ToString("chp", Guid.CreateVersion7());
		Assert.Equal("chp", PrefixedId.GetPrefix(prefixed));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("noguid")]
	public void GetPrefix_WithInvalidInput_ReturnsNull(string? input) {
		Assert.Null(PrefixedId.GetPrefix(input));
	}

	[Fact]
	public void RoundTrip_ToString_ThenToGuid_IsIdentity() {
		var id = Guid.CreateVersion7();
		Assert.Equal(id, PrefixedId.ToGuid(PrefixedId.ToString("x", id)));
	}
}
