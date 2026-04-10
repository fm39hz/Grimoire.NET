namespace Grimoire.Infrastructure.Export.Epub;

using System.IO.Compression;
using System.Text;
using System.Web;
using Common;

/// <summary>
///     Builds EPUB package from HTML content
/// </summary>
public class EpubPackageBuilder {
	private readonly List<NavPoint> _navPoints = [];
	private readonly Dictionary<string, EpubResource> _resources = new();
	private string? _author;
	private string? _coverImagePath;
	private string? _description;
	private string? _language = EpubConstants.Defaults.Language;
	private List<string>? _tags;
	private string? _title;

	public void SetMetadata(string title, string? author = null, string? language = null, string? description = null,
		List<string>? tags = null) {
		_title = title;
		_author = author;
		_language = language ?? EpubConstants.Defaults.Language;
		_description = description;
		_tags = tags;
	}

	public void SetCoverImage(string relativePath) => _coverImagePath = relativePath;

	public void AddResource(EpubResource resource) => _resources[resource.Path] = resource;

	public void AddHtmlFile(string path, string content) => AddResource(EpubResource.FromText(path, content));

	public void AddImageFile(string path, byte[] content) => AddResource(EpubResource.FromBytes(path, content));

	public void AddImageFileStream(string path, Func<Task<Stream?>> streamProvider) =>
		AddResource(EpubResource.FromStream(path, streamProvider));

	public void AddNavPoint(NavPoint navPoint) => _navPoints.Add(navPoint);

	public List<NavPoint> GetNavPoints() => _navPoints;

	/// <summary>
	///     Flattens the NavPoint tree into a linear list of ContentSrc paths in reading order
	/// </summary>
	private List<string> FlattenNavPoints() {
		var result = new List<string>();

		void flatten(NavPoint nav) {
			if (!string.IsNullOrEmpty(nav.ContentSrc)) {
				result.Add(nav.ContentSrc);
			}

			if (nav.Children != null) {
				foreach (var child in nav.Children) {
					flatten(child);
				}
			}
		}

		foreach (var nav in _navPoints) {
			flatten(nav);
		}

		return result;
	}

	public void AddCss(string content) => AddResource(EpubResource.FromText(EpubConstants.Paths.StyleCssFile, content));

	public async Task<Stream> BuildAsync() {
		// Generate required EPUB files
		GenerateContainerXml();
		GenerateContentOpf();
		GenerateTocNcx();

		// Create ZIP package
		var memoryStream = new MemoryStream();
		using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true)) {
			// Add mimetype file (uncompressed, first file)
			var mimetypeEntry = archive.CreateEntry(EpubConstants.Paths.MimeTypeFile, CompressionLevel.NoCompression);
			using (var writer = new StreamWriter(mimetypeEntry.Open(), Encoding.ASCII)) {
				writer.Write("application/epub+zip");
			}

			// Add all resources
			foreach (var resource in _resources.Values) {
				await AddResourceToArchive(archive, resource);
			}
		}

		memoryStream.Position = 0;
		return memoryStream;
	}

	private static async Task AddResourceToArchive(ZipArchive archive, EpubResource resource) {
		var entry = archive.CreateEntry(resource.Path, CompressionLevel.Optimal);

		switch (resource.Type) {
			case EpubResourceType.Text:
				using (var writer = new StreamWriter(entry.Open(), Encoding.UTF8)) {
					writer.Write(resource.TextContent);
				}

				break;

			case EpubResourceType.Binary:
				using (var stream = entry.Open()) {
					stream.Write(resource.BinaryContent!, 0, resource.BinaryContent!.Length);
				}

				break;

			case EpubResourceType.Stream:
				var sourceStream = await resource.StreamProvider!();
				if (sourceStream != null) {
					try {
						using var entryStream = entry.Open();
						await sourceStream.CopyToAsync(entryStream);
					}
					finally {
						await sourceStream.DisposeAsync();
					}
				}

				break;
		}
	}

	private void GenerateContainerXml() {
		var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<container version=""1.0"" xmlns=""urn:oasis:names:tc:opendocument:xmlns:container"">
  <rootfiles>
    <rootfile full-path=""OEBPS/content.opf"" media-type=""application/oebps-package+xml""/>
  </rootfiles>
</container>";
		AddResource(EpubResource.FromText(EpubConstants.Paths.ContainerXmlFile, xml));
	}

	private void GenerateContentOpf() {
		var sb = new StringBuilder();
		sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
		sb.AppendLine("<package xmlns=\"http://www.idpf.org/2007/opf\" version=\"3.0\" unique-identifier=\"uid\">");

		// Metadata
		sb.AppendLine("  <metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\">");
		sb.AppendLine($"    <dc:identifier id=\"uid\">{Guid.NewGuid()}</dc:identifier>");
		sb.AppendLine(
			$"    <dc:title>{HttpUtility.HtmlEncode(_title ?? EpubConstants.Defaults.UntitledBook)}</dc:title>");
		if (!string.IsNullOrEmpty(_author)) {
			sb.AppendLine($"    <dc:creator>{HttpUtility.HtmlEncode(_author)}</dc:creator>");
		}

		if (!string.IsNullOrEmpty(_description)) {
			sb.AppendLine($"    <dc:description>{HttpUtility.HtmlEncode(_description)}</dc:description>");
		}

		if (_tags is { Count: > 0 }) {
			foreach (var tag in _tags) {
				sb.AppendLine($"    <dc:subject>{HttpUtility.HtmlEncode(tag)}</dc:subject>");
			}
		}

		sb.AppendLine($"    <dc:language>{_language}</dc:language>");
		sb.AppendLine($"    <meta property=\"dcterms:modified\">{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</meta>");

		// Add cover image metadata if present
		if (!string.IsNullOrEmpty(_coverImagePath)) {
			sb.AppendLine("    <meta name=\"cover\" content=\"cover-image\"/>");
		}

		sb.AppendLine("  </metadata>");

		// Manifest
		sb.AppendLine("  <manifest>");
		sb.AppendLine(
			"    <item id=\"nav\" href=\"nav.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\"/>");
		sb.AppendLine("    <item id=\"ncx\" href=\"toc.ncx\" media-type=\"application/x-dtbncx+xml\"/>");
		sb.AppendLine("    <item id=\"css\" href=\"style.css\" media-type=\"text/css\"/>");

		// Add HTML files - order follows NavPoints (authoritative reading order)
		var fileIndex = 1;
		var navOrder = FlattenNavPoints();
		var navFile = EpubConstants.Paths.NavFile;

		var htmlFiles = navOrder
			.Select(contentSrc => $"{EpubConstants.Paths.OebpsPrefix}{contentSrc}")
			.Where(_resources.ContainsKey)
			.Where(path => path != navFile) // Exclude nav.xhtml - already in manifest with id="nav"
			.ToList();

		foreach (var path in htmlFiles) {
			var filename = path.Replace(EpubConstants.Paths.OebpsPrefix, "");
			var id = $"file{fileIndex++}";
			sb.AppendLine($"    <item id=\"{id}\" href=\"{filename}\" media-type=\"application/xhtml+xml\"/>");
		}

		// Add image files
		var imageIndex = 1;
		var imagePaths = _resources.Keys
			.Where(k => k.StartsWith($"{EpubConstants.Paths.OebpsPrefix}{EpubConstants.Paths.ImagesFolder}"))
			.ToList();

		foreach (var path in imagePaths) {
			var filename = path.Replace(EpubConstants.Paths.OebpsPrefix, "");
			var extension = Path.GetExtension(filename).ToLowerInvariant();
			var mediaType = extension switch {
				".jpg" or ".jpeg" => "image/jpeg",
				".png" => "image/png",
				".gif" => "image/gif",
				".webp" => "image/webp",
				".svg" => "image/svg+xml",
				_ => "image/jpeg"
			};

			// Check if this is the cover image
			var isCover = !string.IsNullOrEmpty(_coverImagePath) && filename == _coverImagePath;
			var itemId = isCover? "cover-image" : $"img{imageIndex++}";
			var properties = isCover? " properties=\"cover-image\"" : "";

			sb.AppendLine($"    <item id=\"{itemId}\" href=\"{filename}\" media-type=\"{mediaType}\"{properties}/>");
		}

		sb.AppendLine("  </manifest>");

		// Spine - follows NavPoints order (authoritative reading sequence)
		sb.AppendLine("  <spine toc=\"ncx\">");
		fileIndex = 1;

		foreach (var contentSrc in navOrder) {
			var fullPath = $"{EpubConstants.Paths.OebpsPrefix}{contentSrc}";

			// Skip if resource doesn't exist
			if (!_resources.ContainsKey(fullPath)) {
				continue;
			}

			// Use special id for nav.xhtml
			var itemRef = contentSrc == "nav.xhtml"? "nav" : $"file{fileIndex++}";
			sb.AppendLine($"    <itemref idref=\"{itemRef}\"/>");
		}

		sb.AppendLine("  </spine>");

		sb.AppendLine("</package>");
		AddResource(EpubResource.FromText(EpubConstants.Paths.ContentOpfFile, sb.ToString()));
	}

	private void GenerateTocNcx() {
		var sb = new StringBuilder();
		sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
		sb.AppendLine("<ncx xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" version=\"2005-1\">");
		sb.AppendLine("  <head>");
		sb.AppendLine($"    <meta name=\"dtb:uid\" content=\"{Guid.NewGuid()}\"/>");
		sb.AppendLine("    <meta name=\"dtb:depth\" content=\"2\"/>");
		sb.AppendLine("  </head>");
		sb.AppendLine(
			$"  <docTitle><text>{HttpUtility.HtmlEncode(_title ?? EpubConstants.Defaults.UntitledBook)}</text></docTitle>");
		sb.AppendLine("  <navMap>");

		var playOrder = 1;
		foreach (var nav in _navPoints) {
			// Skip nav.xhtml to avoid self-reference in NCX TOC
			if (nav.ContentSrc == "nav.xhtml") {
				continue;
			}

			sb.AppendLine(RenderNavPointNcx(nav, ref playOrder));
		}

		sb.AppendLine("  </navMap>");
		sb.AppendLine("</ncx>");
		AddResource(EpubResource.FromText(EpubConstants.Paths.TocNcxFile, sb.ToString()));
	}

	private static string RenderNavPointNcx(NavPoint nav, ref int playOrder) {
		var sb = new StringBuilder();
		sb.AppendLine($"    <navPoint id=\"nav{playOrder}\" playOrder=\"{playOrder}\">");
		sb.AppendLine($"      <navLabel><text>{HttpUtility.HtmlEncode(nav.Title)}</text></navLabel>");
		sb.AppendLine($"      <content src=\"{nav.ContentSrc}\"/>");

		playOrder++;

		if (nav.Children != null) {
			foreach (var child in nav.Children) {
				sb.Append(RenderNavPointNcx(child, ref playOrder));
			}
		}

		sb.AppendLine("    </navPoint>");
		return sb.ToString();
	}
}
