namespace Grimoire.Infrastructure.Import;

using Application.Import;
using Microsoft.Extensions.Logging;
using VersOne.Epub;

public sealed class EpubParser(ILogger<EpubParser> logger) : IEpubParser {
    public async Task<EpubParseResult> ParseAsync(Stream epubStream, CancellationToken cancellationToken = default) {
        var book = await EpubReader.ReadBookAsync(epubStream);

        var result = new EpubParseResult {
            Title = book.Title ?? "Untitled",
            Author = book.Author ?? book.AuthorList?.FirstOrDefault(),
            Description = book.Description,
            CoverBytes = book.CoverImage,
            CoverContentType = "image/jpeg",
            Images = ExtractImages(book)
        };

        var tocItems = book.Navigation ?? [];
        var title = result.Title;

        if (tocItems.Count > 0) {
            result.Volumes = BuildVolumeTree(tocItems, title);
        }
        else {
            var volume = new EpubVolumeEntry { Order = 1, Title = title, Chapters = [] };

            var order = 1;
            foreach (var item in book.ReadingOrder) {
                var chTitle = ExtractTitleFromHtml(item.Content) ?? $"Chapter {order}";
                volume.Chapters.Add(new EpubChapterEntry {
                    Order = order++,
                    Title = chTitle,
                    HtmlContent = item.Content,
                    SourceHref = item.Key
                });
            }

            if (volume.Chapters.Count > 0)
                result.Volumes.Add(volume);
        }

        foreach (var vol in result.Volumes)
            foreach (var ch in vol.Chapters)
                result.ChapterHtmlMap[$"{vol.Order}:{ch.Order}"] = ch.HtmlContent;

        logger.LogInformation(
            "Parsed EPUB: {Title}, {VolumeCount} volumes, {ChapterCount} chapters, {ImageCount} images",
            result.Title, result.Volumes.Count,
            result.Volumes.Sum(v => v.Chapters.Count), result.Images.Count);

        return result;
    }

    private static Dictionary<string, byte[]> ExtractImages(EpubBook book) {
        var images = new Dictionary<string, byte[]>();
        foreach (var image in book.Content.Images.Local)
            images[image.Key] = image.Content;
        return images;
    }

    private static List<EpubVolumeEntry> BuildVolumeTree(
        List<EpubNavigationItem> navItems, string fallbackTitle) {

        var volumes = new List<EpubVolumeEntry>();
        var volOrder = 1;

        foreach (var item in navItems) {
            if (item.NestedItems.Count > 0) {
                var volume = new EpubVolumeEntry {
                    Order = volOrder++,
                    Title = item.Title ?? fallbackTitle,
                    Chapters = []
                };

                var chOrder = 1;
                foreach (var child in item.NestedItems) {
                    volume.Chapters.Add(new EpubChapterEntry {
                        Order = chOrder++,
                        Title = child.Title ?? $"Chapter {volume.Chapters.Count + 1}",
                        HtmlContent = child.HtmlContentFile?.Content ?? string.Empty,
                        SourceHref = child.Link?.ContentFileUrl
                    });
                }
                volumes.Add(volume);
            }
            else {
                volumes.Add(new EpubVolumeEntry {
                    Order = volOrder++,
                    Title = item.Title ?? fallbackTitle,
                    Chapters = [
                        new EpubChapterEntry {
                            Order = 1,
                            Title = item.Title ?? "Chapter 1",
                            HtmlContent = item.HtmlContentFile?.Content ?? string.Empty,
                            SourceHref = item.Link?.ContentFileUrl
                        }
                    ]
                });
            }
        }

        return volumes;
    }

    private static string? ExtractTitleFromHtml(string html) {
        const string tag = "<title>";
        const string end = "</title>";
        var s = html.IndexOf(tag, StringComparison.OrdinalIgnoreCase);
        if (s < 0) return null;
        s += tag.Length;
        var e = html.IndexOf(end, s, StringComparison.OrdinalIgnoreCase);
        return e < 0 ? null : html[s..e].Trim();
    }
}
