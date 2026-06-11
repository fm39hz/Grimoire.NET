namespace Grimoire.Application.Import;

using Dto.Book;

public sealed class EpubImportStrategy(
    IEpubParser epubParser,
    IXhtmlSegmentParser xhtmlParser) : IImportStrategy {

    public string Format => "epub";

    public bool CanHandle(string fileName) =>
        fileName.EndsWith(".epub", StringComparison.OrdinalIgnoreCase);

    public async Task<NormalizedImport> ParseAsync(Stream source, CancellationToken cancellationToken = default)
    {
        var raw = await epubParser.ParseAsync(source, cancellationToken);

        var files = raw.Images.Select(kvp => new NormalizedFile
        {
            FileName = kvp.Key,
            Content = kvp.Value,
            ContentType = DetectMime(kvp.Key)
        }).ToList();

        var volumes = raw.Volumes.Select(v => new NormalizedVolume
        {
            Order = v.Order,
            Title = v.Title,
            Chapters = v.Chapters.Select(c =>
            {
                var parsed = !string.IsNullOrEmpty(c.HtmlContent)
                    ? xhtmlParser.Parse(c.HtmlContent, raw.Images)
                    : new ParsedChapter();

                return new NormalizedChapter
                {
                    Order = c.Order,
                    Title = c.Title,
                    Segments = parsed.Segments,
                    Footnotes = parsed.Footnotes,
                    RawHtml = c.HtmlContent
                };
            }).ToList()
        }).ToList();

        return new NormalizedImport
        {
            Title = raw.Title,
            Author = raw.Author,
            Description = raw.Description,
            Tags = raw.Tags,
            Cover = raw.CoverBytes?.Length > 0
                ? new NormalizedFile
                {
                    FileName = "cover",
                    Content = raw.CoverBytes,
                    ContentType = raw.CoverContentType ?? "image/jpeg"
                }
                : null,
            Volumes = volumes,
            Files = files
        };
    }

    private static string DetectMime(string name) =>
        Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
}
