namespace Grimoire.Infrastructure.Export.Epub;

using System.IO.Compression;
using System.Text;
using System.Web;

/// <summary>
///     Builds EPUB package from HTML content
/// </summary>
public class EpubPackageBuilder {
	private readonly Dictionary<string, byte[]> _binaryFiles = new();
	private readonly Dictionary<string, Func<Task<Stream?>>> _binaryFileStreamProviders = new();
	private readonly Dictionary<string, string> _files = new();
	private readonly List<NavPoint> _navPoints = [];
	private string? _author;
	private string? _coverImagePath;
	private string? _description;
	private string? _language = "vi";
	private string? _title;

	public void SetMetadata(string title, string? author = null, string? language = null, string? description = null) {
		_title = title;
		_author = author;
		_language = language ?? "vi";
		_description = description;
	}

	public void SetCoverImage(string relativePath) => _coverImagePath = relativePath;

	public void AddHtmlFile(string path, string content) => _files[path] = content;

	public void AddImageFile(string path, byte[] content) => _binaryFiles[path] = content;

	public void AddImageFileStream(string path, Func<Task<Stream?>> streamProvider) =>
		_binaryFileStreamProviders[path] = streamProvider;

	public void AddNavPoint(NavPoint navPoint) => _navPoints.Add(navPoint);

	public List<NavPoint> GetNavPoints() => _navPoints;

	public void AddCss(string content) => _files["OEBPS/style.css"] = content;

	public async Task<Stream> BuildAsync() {
		// Generate required EPUB files
		GenerateContainerXml();
		GenerateContentOpf();
		GenerateTocNcx();

		// Create ZIP package
		var memoryStream = new MemoryStream();
		using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true)) {
			// Add mimetype file (uncompressed, first file)
			var mimetypeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
			using (var writer = new StreamWriter(mimetypeEntry.Open(), Encoding.ASCII)) {
				writer.Write("application/epub+zip");
			}

			// Add all text files
			foreach (var (path, content) in _files) {
				var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
				using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
				writer.Write(content);
			}

			// Add all binary files (images from byte arrays)
			foreach (var (path, content) in _binaryFiles) {
				var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
				using var stream = entry.Open();
				stream.Write(content, 0, content.Length);
			}

			// Add all binary files (images from streams)
			foreach (var (path, streamProvider) in _binaryFileStreamProviders) {
				var sourceStream = await streamProvider();
				if (sourceStream == null) {
					continue;
				}

				try {
					var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
					using var entryStream = entry.Open();
					await sourceStream.CopyToAsync(entryStream);
				}
				finally {
					await sourceStream.DisposeAsync();
				}
			}
		}

		memoryStream.Position = 0;
		return memoryStream;
	}

	private void GenerateContainerXml() {
		var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<container version=""1.0"" xmlns=""urn:oasis:names:tc:opendocument:xmlns:container"">
  <rootfiles>
    <rootfile full-path=""OEBPS/content.opf"" media-type=""application/oebps-package+xml""/>
  </rootfiles>
</container>";
		_files["META-INF/container.xml"] = xml;
	}

	private void GenerateContentOpf() {
		var sb = new StringBuilder();
		sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
		sb.AppendLine("<package xmlns=\"http://www.idpf.org/2007/opf\" version=\"3.0\" unique-identifier=\"uid\">");

		// Metadata
		sb.AppendLine("  <metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\">");
		sb.AppendLine($"    <dc:identifier id=\"uid\">{Guid.NewGuid()}</dc:identifier>");
		sb.AppendLine($"    <dc:title>{HttpUtility.HtmlEncode(_title ?? "Untitled")}</dc:title>");
		if (!string.IsNullOrEmpty(_author)) {
			sb.AppendLine($"    <dc:creator>{HttpUtility.HtmlEncode(_author)}</dc:creator>");
		}

		if (!string.IsNullOrEmpty(_description)) {
			sb.AppendLine($"    <dc:description>{HttpUtility.HtmlEncode(_description)}</dc:description>");
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

		// Add HTML files
		var fileIndex = 1;
		foreach (var path in _files.Keys.Where(k => k.StartsWith("OEBPS/") && k.EndsWith(".xhtml"))) {
			var filename = path.Replace("OEBPS/", "");
			var id = $"file{fileIndex++}";
			sb.AppendLine($"    <item id=\"{id}\" href=\"{filename}\" media-type=\"application/xhtml+xml\"/>");
		}

		// Add image files
		var imageIndex = 1;
		var allImagePaths = _binaryFiles.Keys.Concat(_binaryFileStreamProviders.Keys)
			.Where(k => k.StartsWith("OEBPS/images/"));

		foreach (var path in allImagePaths) {
			var filename = path.Replace("OEBPS/", "");
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

		// Spine
		sb.AppendLine("  <spine toc=\"ncx\">");
		fileIndex = 1;
		foreach (var path in _files.Keys.Where(k => k.StartsWith("OEBPS/") && k.EndsWith(".xhtml")).OrderBy(k => k)) {
			sb.AppendLine($"    <itemref idref=\"file{fileIndex++}\"/>");
		}

		sb.AppendLine("  </spine>");

		sb.AppendLine("</package>");
		_files["OEBPS/content.opf"] = sb.ToString();
	}

	private void GenerateTocNcx() {
		var sb = new StringBuilder();
		sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
		sb.AppendLine("<ncx xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" version=\"2005-1\">");
		sb.AppendLine("  <head>");
		sb.AppendLine($"    <meta name=\"dtb:uid\" content=\"{Guid.NewGuid()}\"/>");
		sb.AppendLine("    <meta name=\"dtb:depth\" content=\"2\"/>");
		sb.AppendLine("  </head>");
		sb.AppendLine($"  <docTitle><text>{HttpUtility.HtmlEncode(_title ?? "Untitled")}</text></docTitle>");
		sb.AppendLine("  <navMap>");

		var playOrder = 1;
		foreach (var nav in _navPoints) {
			sb.AppendLine(RenderNavPointNcx(nav, ref playOrder));
		}

		sb.AppendLine("  </navMap>");
		sb.AppendLine("</ncx>");
		_files["OEBPS/toc.ncx"] = sb.ToString();
	}

	private string RenderNavPointNcx(NavPoint nav, ref int playOrder) {
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
