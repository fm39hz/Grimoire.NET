namespace Grimoire.Tests.Application;

using Grimoire.Application.Dto.Book;
using Grimoire.Application.Dto.Book.Validators;
using Grimoire.Application.Service.Strategy;
using System.Collections.Generic;
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
			TargetVolumeIds = new List<string> { "vol_187661b1-2e65-4f40-84a1-b843d22b2707" }
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
		Assert.Contains(result.Errors, e => e.PropertyName == nameof(BinderyRequestDto.TargetVolumeIds));
	}

	[Fact]
	public void Should_Fail_When_TargetVolumeIds_Have_Invalid_Prefix() {
		var request = new BinderyRequestDto {
			Format = ExportFormat.Epub,
			Mode = "Single",
			TargetVolumeIds = new List<string> { "vol187661b1-2e65-4f40-84a1-b843d22b2707", "ser_187661b1-2e65-4f40-84a1-b843d22b2707" }
		};

		var result = _validator.Validate(request);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, e => e.PropertyName.StartsWith(nameof(BinderyRequestDto.TargetVolumeIds)));
	}
}
