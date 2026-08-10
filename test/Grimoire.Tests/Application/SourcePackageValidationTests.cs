namespace Grimoire.Tests.Application;

using Grimoire.Application.Ingestion.Contract;
using Grimoire.Application.Ingestion.Reconciliation;
using Grimoire.Domain.Entity.Ingestion;
using Xunit;

public sealed class SourcePackageValidationTests {
	[Fact]
	public void DuplicateExternalNodeKeysAreRejectedAcrossTheWholeTree() {
		var package = new SourcePackageDto(
			"1.0",
			new SourceProducerDto("producer", "1"),
			"run-1",
			new SourceDescriptorDto("source", "test"),
			null,
			ImportSemantics.Patch,
			[
				new SourceNodeDto("same", ReconciliationNodeKind.Container, "Volume", Children: [
					new SourceNodeDto("same", ReconciliationNodeKind.Content, "Chapter")
				])
			]);

		var result = new SourcePackageDtoValidator().Validate(package);

		Assert.False(result.IsValid);
		Assert.Contains(result.Errors, static error => error.ErrorMessage.Contains("duplicated", StringComparison.Ordinal));
	}
}
