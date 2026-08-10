namespace Grimoire.Tests.Application;

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Publish.Export;
using Grimoire.Application.Publish.Export.Steps;
using Grimoire.Application.Service.Contract;
using Grimoire.Application.Service.Strategy;
using Grimoire.Domain.Common;
using Grimoire.Domain.Entity.Book;
using Grimoire.Tests.TestInfrastructure;
using Xunit;

public sealed class PublishBatchTests {
	[Fact]
	public async Task OnePerVolume_ProducesZipWithArtifactsAndManifest() {
		var seriesRepository = new InMemorySeriesRepository();
		var volumeRepository = new InMemoryVolumeRepository();
		var series = new SeriesModel { Id = Guid.NewGuid(), Title = "Series" };
		series.Path = $"n{series.Id:N}";
		await seriesRepository.Create(series);
		var first = new VolumeModel { Id = Guid.NewGuid(), Title = "Volume 1", Order = 1, Path = $"{series.Path}.n{Guid.NewGuid():N}" };
		var second = new VolumeModel { Id = Guid.NewGuid(), Title = "Volume 2", Order = 2, Path = $"{series.Path}.n{Guid.NewGuid():N}" };
		// Paths carry ownership, so use each actual volume ID in the final node component.
		first.Path = $"{series.Path}.n{first.Id:N}";
		second.Path = $"{series.Path}.n{second.Id:N}";
		await volumeRepository.Create(first);
		await volumeRepository.Create(second);
		var bindery = new FakeBindery();
		var step = new ContentGenerationStep(bindery, volumeRepository, seriesRepository);
		var context = new ExportPipelineContext(series.Id, new BinderyRequestDto { Mode = "OnePerVolume" }, "job-1");

		await step.ExecuteAsync(context, default);

		Assert.NotNull(context.ExportResult);
		Assert.True(context.ExportResult.Success);
		Assert.Equal("application/zip", context.ExportResult.ContentType);
		Assert.Equal(2, context.Artifacts?.Count);
		Assert.All(bindery.Requests, static request => Assert.Equal("Single", request.Mode));
		Assert.All(bindery.Requests, static request => Assert.Single(request.TargetVolumeIds!));

		using var archive = new ZipArchive(context.ExportResult.ContentStream, ZipArchiveMode.Read);
		Assert.Equal(3, archive.Entries.Count);
		var manifestEntry = Assert.Single(archive.Entries, static entry => entry.FullName == "manifest.json");
		await using var manifestStream = manifestEntry.Open();
		using var manifest = await JsonDocument.ParseAsync(manifestStream);
		Assert.Equal("grimoire.publish-manifest.v1", manifest.RootElement.GetProperty("Schema").GetString());
		Assert.Equal(2, manifest.RootElement.GetProperty("Artifacts").GetArrayLength());
	}

	private sealed class FakeBindery : IBinderyService {
		public List<BinderyRequestDto> Requests { get; } = [];

		public Task<ExportResult> ExportSeriesAsync(Guid seriesId, BinderyRequestDto request, CancellationToken cancellationToken = default) {
			Requests.Add(request);
			var bytes = Encoding.UTF8.GetBytes(request.TargetVolumeIds!.Single());
			return Task.FromResult(ExportResult.Ok(new MemoryStream(bytes), "book.epub", "application/epub+zip"));
		}
	}
}
