namespace Grimoire.Infrastructure.Import;

using System.Linq;
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
            Images = ExtractImages(book),
            Tags = book.Schema?.Package?.Metadata?.Subjects?
                .Select(s => s.Subject)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList() ?? []
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

        // 1. If the TOC has no nesting at all (flat list of chapters)
        var hasAnyNesting = navItems.Any(item => item.NestedItems.Count > 0);
        if (!hasAnyNesting) {
            var singleVol = new EpubVolumeEntry {
                Order = volOrder++,
                Title = fallbackTitle,
                Chapters = []
            };
            var chOrder = 1;
            foreach (var item in navItems) {
                singleVol.Chapters.Add(new EpubChapterEntry {
                    Order = chOrder++,
                    Title = item.Title ?? $"Chapter {chOrder}",
                    HtmlContent = item.HtmlContentFile?.Content ?? string.Empty,
                    SourceHref = item.Link?.ContentFileUrl
                });
            }
            volumes.Add(singleVol);
            return volumes;
        }

        // 2. If there is nesting, recursively discover Volumes and Chapters
        var orphanRootChapters = new List<EpubChapterEntry>();
        var orphanChOrder = 1;

        foreach (var item in navItems) {
            if (item.NestedItems.Count > 0) {
                FindVolumeRoots(item, volumes, ref volOrder, "");
            }
            else {
                orphanRootChapters.Add(new EpubChapterEntry {
                    Order = orphanChOrder++,
                    Title = item.Title ?? $"Chapter {orphanChOrder}",
                    HtmlContent = item.HtmlContentFile?.Content ?? string.Empty,
                    SourceHref = item.Link?.ContentFileUrl
                });
            }
        }

        // If there were any leaf nodes at the root level, group them into a Front Matter volume
        if (orphanRootChapters.Count > 0) {
            var frontMatterVol = new EpubVolumeEntry {
                Order = 1,
                Title = $"{fallbackTitle} - Front Matter",
                Chapters = orphanRootChapters
            };
            volumes.Insert(0, frontMatterVol);
            
            // Re-adjust order values by reconstructing volumes to bypass init-only restriction
            for (int i = 0; i < volumes.Count; i++) {
                var v = volumes[i];
                volumes[i] = new EpubVolumeEntry {
                    Order = i + 1,
                    Title = v.Title,
                    Chapters = v.Chapters
                };
            }
        }

        return volumes;
    }

    private static void FindVolumeRoots(
        EpubNavigationItem node,
        List<EpubVolumeEntry> volumes,
        ref int volOrder,
        string parentTitlePrefix) {

        var title = string.IsNullOrEmpty(parentTitlePrefix)
            ? (node.Title ?? "Untitled")
            : $"{parentTitlePrefix} - {node.Title}";

        // Check if any of this node's children have children of their own
        var hasSubGroups = node.NestedItems.Any(child => child.NestedItems.Count > 0);

        if (hasSubGroups) {
            // This node is a group container.
            // Group any direct leaf children of this node into a Front Matter volume.
            var leafChildren = new List<EpubChapterEntry>();
            var chOrder = 1;
            foreach (var child in node.NestedItems) {
                if (child.NestedItems.Count == 0) {
                    leafChildren.Add(new EpubChapterEntry {
                        Order = chOrder++,
                        Title = child.Title ?? $"Chapter {chOrder}",
                        HtmlContent = child.HtmlContentFile?.Content ?? string.Empty,
                        SourceHref = child.Link?.ContentFileUrl
                    });
                }
            }

            if (leafChildren.Count > 0) {
                volumes.Add(new EpubVolumeEntry {
                    Order = volOrder++,
                    Title = $"{title} - Front Matter",
                    Chapters = leafChildren
                });
            }

            // Recurse into children that have sub-items
            foreach (var child in node.NestedItems) {
                if (child.NestedItems.Count > 0) {
                    FindVolumeRoots(child, volumes, ref volOrder, title);
                }
            }
        }
        else {
            // This node is a leaf-parent (a Volume Root). All its children are chapters.
            var volume = new EpubVolumeEntry {
                Order = volOrder++,
                Title = title,
                Chapters = []
            };

            // Also check if the node itself has content (like a volume intro/cover page)
            var hasContent = !string.IsNullOrEmpty(node.HtmlContentFile?.Content) ||
                             !string.IsNullOrEmpty(node.Link?.ContentFileUrl);
            var chOrder = 1;
            if (hasContent) {
                volume.Chapters.Add(new EpubChapterEntry {
                    Order = chOrder++,
                    Title = node.Title ?? "Intro",
                    HtmlContent = node.HtmlContentFile?.Content ?? string.Empty,
                    SourceHref = node.Link?.ContentFileUrl
                });
            }

            foreach (var child in node.NestedItems) {
                volume.Chapters.Add(new EpubChapterEntry {
                    Order = chOrder++,
                    Title = child.Title ?? $"Chapter {chOrder}",
                    HtmlContent = child.HtmlContentFile?.Content ?? string.Empty,
                    SourceHref = child.Link?.ContentFileUrl
                });
            }

            volumes.Add(volume);
        }
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
