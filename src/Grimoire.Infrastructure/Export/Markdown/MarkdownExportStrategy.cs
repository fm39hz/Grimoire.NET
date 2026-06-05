namespace Grimoire.Infrastructure.Export.Markdown;

using System.Text;
using System.Threading;
using Application.Export;
using Application.Service.Strategy;
using Common;
using Domain.Entity.Book;
using Microsoft.Extensions.Logging;

public partial class MarkdownExportStrategy(
	ILogger<MarkdownExportStrategy> logger,
	ISectionRendererFactory sectionRendererFactory) : IExportStrategy {
	public ExportFormat Format => ExportFormat.Markdown;

	public async Task<ExportResult> ExportAsync(BookExportContext context, CancellationToken cancellationToken = default) {
		try {
			var renderer = sectionRendererFactory.Resolve(Format);

			if (renderer == null) {
				return ExportResult.Fail($"No renderer found for {Format}");
			}

			var navEntries = new List<NavEntry>();
			var pages = new Dictionary<string, string>();

			foreach (var section in context.Structure.Sections) {
				var sectionNav = renderer.RenderSection(context, section, new MarkdownPackageBuilder(pages));
				navEntries.AddRange(sectionNav);
			}

			var fileName = $"{ExportUtilities.SanitizeFileName(context.Series.Title)}.md";
			var content = BuildMarkdownFile(pages, navEntries, context);

			var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
			return ExportResult.Ok(stream, fileName, "text/markdown");
		}
		catch (Exception ex) {
			logger.LogError(ex, "Failed to export series {Id} as Markdown", context.Series.Id);
			return ExportResult.Fail(ex.Message);
		}
	}

	private static string BuildMarkdownFile(
		Dictionary<string, string> pages,
		IReadOnlyList<NavEntry> navEntries,
		BookExportContext context) {
		var sb = new StringBuilder();

		foreach (var entry in FlattenNavEntries(navEntries)) {
			if (pages.TryGetValue(entry.PageId, out var content)) {
				sb.AppendLine(content);
				sb.AppendLine();
				sb.AppendLine("---");
				sb.AppendLine();
			}
		}

		return sb.ToString();
	}

	private static IEnumerable<NavEntry> FlattenNavEntries(IEnumerable<NavEntry> entries) {
		foreach (var entry in entries) {
			yield return entry;
			if (entry.Children != null) {
				foreach (var child in FlattenNavEntries(entry.Children)) {
					yield return child;
				}
			}
		}
	}

	private sealed class MarkdownPackageBuilder(Dictionary<string, string> pages) : IPackageBuilder {
		public void SetMetadata(BookPackageMetadata metadata) {
		}

		public void AddAsset(string resolvedFileName, Func<Task<Stream?>> streamProvider, AssetRefType refType) {
		}

		public string AddPage(string pageId, string htmlContent, PageRole role = PageRole.Chapter) {
			pages[pageId] = htmlContent;
			return $"{pageId}.md";
		}

		public void AddStylesheet(string css) {
		}

		public void SetNavigation(IReadOnlyList<NavEntry> navEntries) {
		}

		public Task<Stream> BuildAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException("MarkdownPackageBuilder does not support BuildAsync");
	}

	[LoggerMessage(LogLevel.Error, "Failed to export series as Markdown")]
	partial void LogExportFailed(Exception ex);
}
