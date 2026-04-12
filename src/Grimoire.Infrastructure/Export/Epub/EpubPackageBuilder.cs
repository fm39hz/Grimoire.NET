namespace Grimoire.Infrastructure.Export.Epub;

using System.IO.Compression;
using System.Text;
using System.Web;
using Common;

/// <summary>
///     Builds EPUB package from HTML content
/// </summary>
public class EpubPackageBuilder(ITemplateEngine templateEngine) {
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
		await GenerateContentOpfAsync();
		await GenerateTocNcxAsync();

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

	private async Task GenerateContentOpfAsync() {
		var navOrder = FlattenNavPoints();
		var navFile = EpubConstants.Paths.NavFile;

		// Prepare manifest items
		var manifestItems = new List<object>();

		// HTML files
		var fileIndex = 1;
		var htmlFiles = navOrder
			.Select(contentSrc => $"{EpubConstants.Paths.OebpsPrefix}{contentSrc}")
			.Where(_resources.ContainsKey)
			.Where(path => path != navFile)
			.ToList();

		foreach (var path in htmlFiles) {
			manifestItems.Add(new {
				Id = $"file{fileIndex++}",
				Href = path.Replace(EpubConstants.Paths.OebpsPrefix, ""),
				MediaType = "application/xhtml+xml"
			});
		}

		// Image files
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

			var isCover = !string.IsNullOrEmpty(_coverImagePath) && filename == _coverImagePath;
			manifestItems.Add(new {
				Id = isCover ? "cover-image" : $"img{imageIndex++}",
				Href = filename,
				MediaType = mediaType,
				Properties = isCover ? "cover-image" : null
			});
		}

		// Prepare spine items
		var spineItems = new List<object>();
		fileIndex = 1;
		foreach (var contentSrc in navOrder) {
			if (!_resources.ContainsKey($"{EpubConstants.Paths.OebpsPrefix}{contentSrc}")) continue;
			spineItems.Add(new { IdRef = contentSrc == "nav.xhtml" ? "nav" : $"file{fileIndex++}" });
		}

		var xml = await templateEngine.RenderAsync("epub_content_opf", new {
			Uid = Guid.NewGuid(),
			Title = _title ?? EpubConstants.Defaults.UntitledBook,
			Author = _author,
			Description = _description,
			Tags = _tags,
			Language = _language,
			ModifiedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
			CoverImagePath = _coverImagePath,
			ManifestItems = manifestItems,
			SpineItems = spineItems
		});

		AddResource(EpubResource.FromText(EpubConstants.Paths.ContentOpfFile, xml));
	}

	private async Task GenerateTocNcxAsync() {
		var flatNavPoints = new List<object>();
		var playOrder = 1;

		void ProcessNavPoint(NavPoint nav, List<object> target) {
			var current = new {
				nav.Title,
				nav.ContentSrc,
				PlayOrder = playOrder++,
				Children = new List<object>()
			};
			target.Add(current);

			if (nav.Children == null) return;
			foreach (var child in nav.Children) {
				ProcessNavPoint(child, (List<object>)current.Children);
			}
		}

		foreach (var nav in _navPoints) {
			ProcessNavPoint(nav, flatNavPoints);
		}

		var xml = await templateEngine.RenderAsync("epub_toc_ncx", new {
			Uid = Guid.NewGuid(),
			Title = _title ?? EpubConstants.Defaults.UntitledBook,
			NavPoints = flatNavPoints
		});

		AddResource(EpubResource.FromText(EpubConstants.Paths.TocNcxFile, xml));
	}
}
