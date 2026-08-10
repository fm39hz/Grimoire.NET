namespace Grimoire.Tests.Application;

using Grimoire.Application.Dto.Book;
using Grimoire.Application.Dto.Book.Validators;
using Grimoire.Application.Service.Strategy;
using Xunit;

public class BinderyRequestDtoValidatorTests {
	private readonly BinderyRequestDtoValidator _validator = new();

	[Fact]
	public void Should_Pass_When_Request_Is_Valid_Anthology() {
		var request = new BinderyRequestDto {
			Format = ExportFormat.Epub,
			Mode = "Anthology",
			TargetVolumeIds = null
		};

		var result = _validator.Validate(request);

		Assert.True(result.IsValid);
	}

	[Fact]
	public void Should_Pass_When_Request_Is_Valid_Single_With_TargetVolumeIds() {
		var request = new BinderyRequestDto {
			Format = ExportFormat.Markdown,
			Mode = "Single",
			TargetVolumeIds = ["vol_187661b1-2e65-4f40-84a1-b843d22b2707"]
		};

		var result = _validator.Validate(request);

		Assert.True(result.IsValid);
	}

	[Fact]
	public void Should_Fail_When_Single_Mode_Has_No_TargetVolumeIds() {
		var request = new BinderyRequestDto {
			Format = ExportFormat.Epub,
			Mode = "Single",
			TargetVolumeIds = null
		};

		var result = _validator.Validate(request);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, static e => e.PropertyName == nameof(BinderyRequestDto.TargetVolumeIds));
	}

	[Fact]
	public void Should_Fail_When_TargetVolumeIds_Have_Invalid_Prefix() {
		var request = new BinderyRequestDto {
			Format = ExportFormat.Epub,
			Mode = "Single",
			TargetVolumeIds = ["vol187661b1-2e65-4f40-84a1-b843d22b2707", "ser_187661b1-2e65-4f40-84a1-b843d22b2707"]
		};

		var result = _validator.Validate(request);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, static e => e.PropertyName.StartsWith(nameof(BinderyRequestDto.TargetVolumeIds)));
	}

	[Fact]
	public void Should_Pass_OnePerVolume_Without_Explicit_Targets() {
		var result = _validator.Validate(new BinderyRequestDto { Mode = "OnePerVolume" });

		Assert.True(result.IsValid);
	}

	[Fact]
	public void Should_Pass_CustomGroups_With_Named_Volume_Sets() {
		var result = _validator.Validate(new BinderyRequestDto {
			Mode = "CustomGroups",
			Groups = [new ExportGroupDto("Main story", ["vol_187661b1-2e65-4f40-84a1-b843d22b2707"])]
		});

		Assert.True(result.IsValid);
	}

	[Fact]
	public void Should_Fail_CustomGroups_With_Duplicate_Names() {
		var result = _validator.Validate(new BinderyRequestDto {
			Mode = "CustomGroups",
			Groups = [
				new ExportGroupDto("Arc", ["vol_187661b1-2e65-4f40-84a1-b843d22b2707"]),
				new ExportGroupDto("arc", ["vol_287661b1-2e65-4f40-84a1-b843d22b2707"])
			]
		});

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, static error => error.PropertyName == nameof(BinderyRequestDto.Groups));
	}
}
