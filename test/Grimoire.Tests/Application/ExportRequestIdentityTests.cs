namespace Grimoire.Tests.Application;

using Grimoire.Application.Dto.Book;
using Grimoire.Application.Publish.Export;
using Xunit;

public sealed class ExportRequestIdentityTests {
	[Fact]
	public void Identity_Is_Stable_When_Target_Order_Changes() {
		var first = ExportRequestIdentity.Create(new BinderyRequestDto {
			Mode = "Single",
			TargetVolumeIds = ["vol_187661b1-2e65-4f40-84a1-b843d22b2707", "vol_287661b1-2e65-4f40-84a1-b843d22b2707"]
		});
		var second = ExportRequestIdentity.Create(new BinderyRequestDto {
			Mode = "single",
			TargetVolumeIds = ["vol_287661b1-2e65-4f40-84a1-b843d22b2707", "vol_187661b1-2e65-4f40-84a1-b843d22b2707"]
		});

		Assert.Equal(first, second);
	}

	[Fact]
	public void Identity_Separates_Anthology_From_Batch() {
		var anthology = ExportRequestIdentity.Create(new BinderyRequestDto { Mode = "Anthology" });
		var batch = ExportRequestIdentity.Create(new BinderyRequestDto { Mode = "OnePerVolume" });

		Assert.NotEqual(anthology, batch);
	}
}
