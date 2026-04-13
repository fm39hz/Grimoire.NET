namespace Grimoire.Infrastructure.Export.Epub;

using System.IO.Compression;
using System.Text;
using Common;
using Domain.Entity.Book;

/// <summary>
///     Builds an EPUB 3 package. All EPUB-specific path layout (OEBPS/, spine
///     ordering, manifest generation) is encapsulated here.
/// </summary>
public class EpubPackageBuilder(ITemplateEngine templateEngine) : IPackageBuilder {
    // Full OEBPS/... path → resource
    private readonly Dictionary<string, EpubResource> _resources = new();

    // Logical pageId → EPUB-relative filename (e.g. "chapter_001.xhtml")
    private readonly Dictionary<string, string> _pageIdToPath = new();

    // Resolved NavPoint tree (built in SetNavigation, used by BuildAsync)
    private readonly List<NavPoint> _navPoints = [];

    private int _volumeIndex = 1;
    private int _chapterIndex = 1;

    private string? _title;
    private string? _author;
    private string? _description;
    private string _language = EpubConstants.Defaults.Language;
    private List<string>? _tags;
    private string? _coverImagePath;
    private Guid? _sharedIdentifier;

    // ── IPackageBuilder ────────────────────────────────────────────────────

    public void SetMetadata(BookPackageMetadata metadata) {
        _title = metadata.Title;
        _author = metadata.Author;
        _language = metadata.Language ?? EpubConstants.Defaults.Language;
        _description = metadata.PlainTextDescription;
        _tags = metadata.Tags?.ToList();
    }

    public void AddAsset(string resolvedFileName, Func<Task<Stream?>> streamProvider, AssetRefType refType) {
        var localPath = $"{EpubConstants.Paths.ImagesFolder}{resolvedFileName}";
        AddResource(EpubResource.FromStream($"{EpubConstants.Paths.OebpsPrefix}{localPath}", streamProvider));

        if (refType == AssetRefType.Cover) {
            _coverImagePath = localPath;
        }
    }

    public void AddPage(string pageId, string htmlContent, PageRole role = PageRole.Chapter) {
        var fileName = ResolveFileName(pageId, role);
        _pageIdToPath[pageId] = fileName;

        // TableOfContents nav document is generated in BuildAsync from the
        // resolved NavPoint tree — don't add a resource for it now.
        if (role == PageRole.TableOfContents) {
            return;
        }

        AddResource(EpubResource.FromText($"{EpubConstants.Paths.OebpsPrefix}{fileName}", htmlContent));
    }

    public void AddStylesheet(string css) =>
        AddResource(EpubResource.FromText(EpubConstants.Paths.StyleCssFile, css));

    public void SetNavigation(IReadOnlyList<NavEntry> navEntries) {
        _navPoints.Clear();
        foreach (var entry in navEntries) {
            _navPoints.Add(ResolveNavEntry(entry));
        }
    }

    public async Task<Stream> BuildAsync() {
        GenerateNavXhtml();
        GenerateContainerXml();
        GenerateContentOpf();
        GenerateTocNcx();

        var memoryStream = new MemoryStream();
        await using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true)) {
            var mimetypeEntry = archive.CreateEntry(EpubConstants.Paths.MimeTypeFile, CompressionLevel.NoCompression);
            await using (var writer = new StreamWriter(await mimetypeEntry.OpenAsync(), Encoding.ASCII)) {
                await writer.WriteAsync("application/epub+zip");
            }

            foreach (var resource in _resources.Values) {
                await AddResourceToArchiveAsync(archive, resource);
            }
        }

        memoryStream.Position = 0;
        return memoryStream;
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private string ResolveFileName(string pageId, PageRole role) => role switch {
        PageRole.Intro => "intro.xhtml",
        PageRole.Description => "description.xhtml",
        PageRole.TableOfContents => "nav.xhtml",
        PageRole.VolumeTitle => $"volume_{_volumeIndex++:D3}.xhtml",
        PageRole.Chapter => $"chapter_{_chapterIndex++:D3}.xhtml",
        _ => $"page_{pageId}.xhtml"
    };

    private NavPoint ResolveNavEntry(NavEntry entry) {
        var contentSrc = _pageIdToPath.TryGetValue(entry.PageId, out var path)
            ? path
            : entry.PageId;

        return new NavPoint {
            Title = entry.Title,
            ContentSrc = contentSrc,
            Children = entry.Children?.Select(ResolveNavEntry).ToList()
        };
    }

    private void GenerateNavXhtml() {
        var html = templateEngine.Render("epub_toc", new { NavPoints = _navPoints });
        AddResource(EpubResource.FromText(EpubConstants.Paths.NavFile, html));
    }

    private void GenerateContainerXml() {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
              <rootfiles>
                <rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml"/>
              </rootfiles>
            </container>
            """;
        AddResource(EpubResource.FromText(EpubConstants.Paths.ContainerXmlFile, xml));
    }

    private void GenerateContentOpf() {
        var navOrder = FlattenNavPoints();

        var manifestItems = new List<object>();
        var fileIndex = 1;

        var htmlFiles = navOrder
            .Select(src => $"{EpubConstants.Paths.OebpsPrefix}{src}")
            .Where(_resources.ContainsKey)
            .Where(p => p != EpubConstants.Paths.NavFile)
            .ToList();

        foreach (var path in htmlFiles) {
            manifestItems.Add(new {
                Id = $"file{fileIndex++}",
                Href = path.Replace(EpubConstants.Paths.OebpsPrefix, ""),
                MediaType = "application/xhtml+xml",
                Properties = (string?)null
            });
        }

        var imageIndex = 1;
        var imagePaths = _resources.Keys
            .Where(k => k.StartsWith($"{EpubConstants.Paths.OebpsPrefix}{EpubConstants.Paths.ImagesFolder}"))
            .ToList();

        foreach (var path in imagePaths) {
            var filename = path.Replace(EpubConstants.Paths.OebpsPrefix, "");
            var ext = Path.GetExtension(filename).ToLowerInvariant();
            var mediaType = ext switch {
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

        var spineItems = new List<object>();
        fileIndex = 1;
        foreach (var src in navOrder.Where(src =>
            _resources.ContainsKey($"{EpubConstants.Paths.OebpsPrefix}{src}"))) {
            spineItems.Add(new { IdRef = src == "nav.xhtml" ? "nav" : $"file{fileIndex++}" });
        }

        _sharedIdentifier = Guid.NewGuid();

        var xml = templateEngine.Render("epub_content_opf", new {
            Uid = _sharedIdentifier.Value,
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

    private void GenerateTocNcx() {
        var flatNavPoints = new List<object>();
        var playOrder = 1;

        foreach (var nav in _navPoints) {
            ProcessNavPoint(nav, flatNavPoints, ref playOrder);
        }

        var xml = templateEngine.Render("epub_toc_ncx", new {
            Uid = _sharedIdentifier ?? Guid.NewGuid(),
            Title = _title ?? EpubConstants.Defaults.UntitledBook,
            NavPoints = flatNavPoints
        });

        AddResource(EpubResource.FromText(EpubConstants.Paths.TocNcxFile, xml));

        void ProcessNavPoint(NavPoint nav, List<object> target, ref int order) {
            var assignedOrder = !string.IsNullOrEmpty(nav.ContentSrc) && nav.ContentSrc != "nav.xhtml"
                ? order++
                : 0;
            var current = new { nav.Title, nav.ContentSrc, PlayOrder = assignedOrder, Children = new List<object>() };
            target.Add(current);
            if (nav.Children == null) return;
            foreach (var child in nav.Children) {
                ProcessNavPoint(child, current.Children, ref order);
            }
        }
    }

    private List<string> FlattenNavPoints() {
        var result = new List<string>();
        foreach (var nav in _navPoints) Flatten(nav, result);
        return result;

        static void Flatten(NavPoint nav, List<string> result) {
            if (!string.IsNullOrEmpty(nav.ContentSrc)) result.Add(nav.ContentSrc);
            if (nav.Children == null) return;
            foreach (var child in nav.Children) Flatten(child, result);
        }
    }

    private void AddResource(EpubResource resource) => _resources[resource.Path] = resource;

    private static async Task AddResourceToArchiveAsync(ZipArchive archive, EpubResource resource) {
        var entry = archive.CreateEntry(resource.Path, CompressionLevel.Optimal);
        switch (resource.Type) {
            case EpubResourceType.Text:
                await using (var writer = new StreamWriter(await entry.OpenAsync(), Encoding.UTF8)) {
                    await writer.WriteAsync(resource.TextContent);
                }
                break;

            case EpubResourceType.Binary:
                await using (var stream = await entry.OpenAsync()) {
#pragma warning disable CA1835
                    await stream.WriteAsync(resource.BinaryContent!, 0, resource.BinaryContent!.Length);
#pragma warning restore CA1835
                }
                break;

            case EpubResourceType.Stream:
                var src = await resource.StreamProvider!();
                if (src != null) {
                    try {
                        await using var entryStream = await entry.OpenAsync();
                        await src.CopyToAsync(entryStream);
                    }
                    finally {
                        await src.DisposeAsync();
                    }
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(resource.Type));
        }
    }
}
