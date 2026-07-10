namespace Grimoire.Application.Publish.Export.Steps;

using System.Threading;
using System.Threading.Tasks;
using Grimoire.Domain.Common.Repository;
using Grimoire.Domain.Entity.Book;

public sealed class StorageUploadStep(
	IStorageRepository storage) : IExportPipelineStep {
	public int Order => 30;

	public async Task ExecuteAsync(ExportPipelineContext context, CancellationToken cancellationToken) {
		if (context.SkipExport || context.ExportResult is null || context.Result is { Success: false }) {
			return;
		}

		var formatDir = context.Request.Format.ToString().ToLowerInvariant();
		var asset = await storage.UploadAssetAsync(
			context.SeriesId,
			context.ExportResult.ContentStream,
			context.ExportResult.ContentType,
			context.ExportResult.FileName,
			AssetRefType.Export,
			prefix: $"staging/export/{formatDir}",
			cancellationToken);

		context.AssetId = asset.Id;
	}
}
