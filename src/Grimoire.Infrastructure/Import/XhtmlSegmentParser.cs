namespace Grimoire.Infrastructure.Import;

using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Application.Dto.Book;
using Application.Import;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;

public sealed class XhtmlSegmentParser : IXhtmlSegmentParser {
    private static readonly HtmlParser Parser = new();

    public ParsedChapter Parse(string html, IReadOnlyDictionary<string, byte[]> images) {
        using var doc = Parser.ParseDocument(html);
        var body = doc.Body ?? doc.DocumentElement;
        var segments = new List<SegmentModel>();
        var footnotes = new List<ImportFootnoteDto>();
        var imageRefs = new List<TempImageReference>();

        var footnoteAsides = new Dictionary<string, IElement>();
        CollectFootnotes(body, footnoteAsides);

        foreach (var node in body.ChildNodes) {
            if (node is IElement el) {
                ProcessElement(el, segments, imageRefs, footnoteAsides, images);
            }
        }

        foreach (var (id, aside) in footnoteAsides) {
            var noteSegments = new List<TextSegmentModel>();
            foreach (var child in aside.ChildNodes) {
                if (child is IElement childEl && childEl.TagName.Equals("P", StringComparison.OrdinalIgnoreCase)) {
                    var runs = ExtractTextRuns(childEl, [], null);
                    if (runs.Count > 0) {
                        noteSegments.Add(new TextSegmentModel { Runs = runs });
                    }
                }
            }
            footnotes.Add(new ImportFootnoteDto {
                InitialId = id,
                Segments = noteSegments
            });
        }

        return new ParsedChapter {
            Segments = segments,
            Footnotes = footnotes,
            Images = imageRefs
        };
    }

    private static void CollectFootnotes(IElement root, Dictionary<string, IElement> footnotes) {
        foreach (var el in root.QuerySelectorAll("aside[epub\\:type='footnote'], aside[epub\\:type=footnote]")) {
            var id = el.Id;
            if (!string.IsNullOrEmpty(id)) {
                footnotes[id] = el;
            }
        }
        foreach (var el in root.QuerySelectorAll("*[epub\\:type='footnote'], *[epub\\:type=footnote]")) {
            if (el.TagName.Equals("ASIDE", StringComparison.OrdinalIgnoreCase)) continue;
            var id = el.Id;
            if (!string.IsNullOrEmpty(id) && !footnotes.ContainsKey(id)) {
                footnotes[id] = el;
            }
        }
    }

    private static void ProcessElement(
        IElement el,
        List<SegmentModel> segments,
        List<TempImageReference> imageRefs,
        Dictionary<string, IElement> footnoteAsides,
        IReadOnlyDictionary<string, byte[]> images) {

        var tag = el.TagName.ToLowerInvariant();
        switch (tag) {
            case "p":
            case "div" when IsBlockTextDiv(el):
                ProcessBlockContent(el, segments, imageRefs, footnoteAsides, images);
                break;

            case "img":
                var src = el.GetAttribute("src");
                if (!string.IsNullOrEmpty(src)) {
                    var normalizedSrc = NormalizeImagePath(src);
                    imageRefs.Add(new TempImageReference {
                        SourceHref = normalizedSrc,
                        AssetKey = images.ContainsKey(normalizedSrc) ? normalizedSrc : src
                    });
                    segments.Add(new ImageSegmentModel {
                        AssetKey = images.ContainsKey(normalizedSrc) ? normalizedSrc : src,
                        Caption = el.GetAttribute("alt")
                    });
                }
                break;

            case "hr":
                segments.Add(new DividerSegmentModel());
                break;

            case "h1":
            case "h2":
            case "h3":
            case "h4":
            case "h5":
            case "h6":
                ProcessBlockContent(el, segments, imageRefs, footnoteAsides, images);
                break;

            case "blockquote":
                ProcessBlockContent(el, segments, imageRefs, footnoteAsides, images);
                break;

            case "div":
            case "section":
                foreach (var child in el.ChildNodes) {
                    if (child is IElement childEl)
                        ProcessElement(childEl, segments, imageRefs, footnoteAsides, images);
                }
                break;

            case "aside":
                if (el.GetAttribute("epub:type") == "footnote") break;
                ProcessBlockContent(el, segments, imageRefs, footnoteAsides, images);
                break;
        }
    }

    private static void ProcessBlockContent(
        IElement parent,
        List<SegmentModel> segments,
        List<TempImageReference> imageRefs,
        Dictionary<string, IElement> footnoteAsides,
        IReadOnlyDictionary<string, byte[]> images) {

        var currentRuns = new List<TextRun>();

        foreach (var node in parent.ChildNodes) {
            if (node is IElement childEl && IsImageOrContainsOnlyImage(childEl, out var imgEl)) {
                if (currentRuns.Count > 0) {
                    segments.Add(new TextSegmentModel { Runs = currentRuns });
                    currentRuns = new List<TextRun>();
                }

                var src = imgEl.GetAttribute("src");
                if (!string.IsNullOrEmpty(src)) {
                    var normalizedSrc = NormalizeImagePath(src);
                    imageRefs.Add(new TempImageReference {
                        SourceHref = normalizedSrc,
                        AssetKey = images.ContainsKey(normalizedSrc) ? normalizedSrc : src
                    });
                    segments.Add(new ImageSegmentModel {
                        AssetKey = images.ContainsKey(normalizedSrc) ? normalizedSrc : src,
                        Caption = imgEl.GetAttribute("alt")
                    });
                }
            }
            else {
                var runs = ExtractTextRunsFromSingleNode(node, imageRefs, footnoteAsides);
                currentRuns.AddRange(runs);
            }
        }

        if (currentRuns.Count > 0) {
            segments.Add(new TextSegmentModel { Runs = currentRuns });
        }
    }

    private static bool IsImageOrContainsOnlyImage(IElement el, out IElement imgEl) {
        if (el.TagName.Equals("IMG", StringComparison.OrdinalIgnoreCase)) {
            imgEl = el;
            return true;
        }

        if (el.ChildNodes.Length == 1 && el.ChildNodes[0] is IElement child) {
            return IsImageOrContainsOnlyImage(child, out imgEl);
        }

        imgEl = null!;
        return false;
    }

    private static List<TextRun> ExtractTextRuns(
        IElement parent,
        List<TempImageReference>? imageRefs,
        Dictionary<string, IElement>? footnoteAsides) {

        var runs = new List<TextRun>();
        foreach (var node in parent.ChildNodes) {
            runs.AddRange(ExtractTextRunsFromSingleNode(node, imageRefs, footnoteAsides));
        }
        return runs;
    }

    private static List<TextRun> ExtractTextRunsFromSingleNode(
        INode node,
        List<TempImageReference>? imageRefs,
        Dictionary<string, IElement>? footnoteAsides) {

        var runs = new List<TextRun>();
        switch (node) {
            case IText text:
                var content = text.Text.Trim();
                if (content.Length > 0)
                    runs.Add(new TextRun(content));
                break;

            case IElement child:
                var childTag = child.TagName.ToLowerInvariant();
                switch (childTag) {
                    case "em":
                    case "i":
                        AddRichRuns(child, runs, imageRefs, footnoteAsides, isItalic: true);
                        break;
                    case "strong":
                    case "b":
                        AddRichRuns(child, runs, imageRefs, footnoteAsides, isBold: true);
                        break;
                    case "a":
                        var href = child.GetAttribute("href") ?? string.Empty;
                        var refType = child.GetAttribute("epub:type");
                        if (refType == "noteref" && href.StartsWith('#')) {
                            var noteId = href.TrimStart('#');
                            var text = child.TextContent.Trim();
                            if (text.Length > 0)
                                runs.Add(new TextRun(text, FootnoteId: noteId));
                        }
                        else {
                            var linkText = child.TextContent.Trim();
                            if (linkText.Length > 0)
                                runs.Add(new TextRun(linkText));
                        }
                        break;
                    case "sup":
                        var supText = child.TextContent.Trim();
                        if (supText.Length > 0)
                            runs.Add(new TextRun(supText));
                        break;
                    case "span":
                        runs.AddRange(ExtractTextRuns(child, imageRefs, footnoteAsides));
                        break;
                    case "img":
                        var src = child.GetAttribute("src");
                        if (!string.IsNullOrEmpty(src))
                            imageRefs?.Add(new TempImageReference {
                                SourceHref = NormalizeImagePath(src),
                                AssetKey = NormalizeImagePath(src)
                            });
                        break;
                }
                break;
        }
        return runs;
    }

    private static void AddRichRuns(
        IElement parent,
        List<TextRun> runs,
        List<TempImageReference>? imageRefs,
        Dictionary<string, IElement>? footnoteAsides,
        bool isBold = false,
        bool isItalic = false) {

        foreach (var node in parent.ChildNodes) {
            switch (node) {
                case IText text:
                    var content = text.Text.Trim();
                    if (content.Length > 0)
                        runs.Add(new TextRun(content, IsBold: isBold, IsItalic: isItalic));
                    break;
                case IElement child:
                    var childTag = child.TagName.ToLowerInvariant();
                    switch (childTag) {
                        case "em":
                        case "i":
                            AddRichRuns(child, runs, imageRefs, footnoteAsides, isBold, true);
                            break;
                        case "strong":
                        case "b":
                            AddRichRuns(child, runs, imageRefs, footnoteAsides, true, isItalic);
                            break;
                        case "a":
                            var href = child.GetAttribute("href") ?? string.Empty;
                            var refType = child.GetAttribute("epub:type");
                            if (refType == "noteref" && href.StartsWith('#')) {
                                var noteId = href.TrimStart('#');
                                var text = child.TextContent.Trim();
                                if (text.Length > 0)
                                    runs.Add(new TextRun(text, IsBold: isBold, IsItalic: isItalic, FootnoteId: noteId));
                            }
                            else {
                                var linkText = child.TextContent.Trim();
                                if (linkText.Length > 0)
                                    runs.Add(new TextRun(linkText, IsBold: isBold, IsItalic: isItalic));
                            }
                            break;
                        default:
                            var defRuns = ExtractTextRuns(child, imageRefs, footnoteAsides);
                            foreach (var r in defRuns)
                                runs.Add(r with { IsBold = r.IsBold || isBold, IsItalic = r.IsItalic || isItalic });
                            break;
                    }
                    break;
            }
        }
    }

    private static bool IsBlockTextDiv(IElement div) {
        var role = div.GetAttribute("role") ?? string.Empty;
        if (role.Contains("doc-")) return true;
        var cls = div.ClassName ?? string.Empty;
        if (cls.Contains("text") || cls.Contains("content") || cls.Contains("body")) return true;
        return !div.HasChildNodes || div.ChildNodes.All(c => c is IText || (c is IElement e && IsInlineElement(e)));
    }

    private static bool IsInlineElement(IElement el) {
        var tag = el.TagName.ToLowerInvariant();
        return tag is "a" or "abbr" or "b" or "cite" or "code" or "em" or "i" or "span"
            or "strong" or "sub" or "sup" or "u" or "img" or "br";
    }

    private static string NormalizeImagePath(string src) {
        return src.Replace('\\', '/').Split('/')[^1];
    }
}
