namespace Grimoire.Infrastructure.Import;

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Application.Dto.Book;
using Application.Import;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;

public sealed class XhtmlSegmentParser : IXhtmlSegmentParser {
    private static readonly HtmlParser Parser = new();
    private readonly Dictionary<string, IInlineTagHandler> _handlers;

    public XhtmlSegmentParser(IEnumerable<IInlineTagHandler> handlers) {
        _handlers = new Dictionary<string, IInlineTagHandler>(StringComparer.OrdinalIgnoreCase);
        foreach (var handler in handlers) {
            foreach (var tag in handler.SupportedTags) {
                _handlers[tag] = handler;
            }
        }
    }

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
                    var childSegments = new List<SegmentModel>();
                    var childRuns = new List<TextRun>();
                    
                    foreach (var node in childEl.ChildNodes) {
                        ParseInlineNode(node, childSegments, childRuns, new FormattingState(), null, images);
                    }
                    
                    if (childRuns.Count > 0) {
                        childSegments.Add(new TextSegmentModel { Runs = childRuns });
                    }
                    
                    foreach (var seg in childSegments) {
                        if (seg is TextSegmentModel textSeg) {
                            noteSegments.Add(textSeg);
                        }
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

    private void ProcessElement(
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

    private void ProcessBlockContent(
        IElement parent,
        List<SegmentModel> segments,
        List<TempImageReference>? imageRefs,
        Dictionary<string, IElement>? footnoteAsides,
        IReadOnlyDictionary<string, byte[]> images) {

        var currentRuns = new List<TextRun>();

        foreach (var node in parent.ChildNodes) {
            ParseInlineNode(node, segments, currentRuns, new FormattingState(), imageRefs, images);
        }

        if (currentRuns.Count > 0) {
            segments.Add(new TextSegmentModel { Runs = currentRuns });
        }
    }

    private void ParseInlineNode(
        INode node,
        List<SegmentModel> segments,
        List<TextRun> currentRuns,
        FormattingState state,
        List<TempImageReference>? imageRefs,
        IReadOnlyDictionary<string, byte[]> images) {

        switch (node) {
            case IText text:
                var content = text.Text.TrimEnd().TrimStart(' ', '\t', '\r', '\n');
                if (content.Length > 0) {
                    currentRuns.Add(new TextRun(content, state.IsBold, state.IsItalic, state.FootnoteId));
                }
                break;

            case IElement child:
                var childTag = child.TagName.ToLowerInvariant();
                if (_handlers.TryGetValue(childTag, out var handler)) {
                    handler.Handle(child, segments, currentRuns, state, imageRefs, images, 
                        (n, s) => ParseInlineNode(n, segments, currentRuns, s, imageRefs, images));
                }
                else {
                    foreach (var grandchild in child.ChildNodes) {
                        ParseInlineNode(grandchild, segments, currentRuns, state, imageRefs, images);
                    }
                }
                break;
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
