namespace Grimoire.Infrastructure.Import;

using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using Grimoire.Domain.Entity.Book;
using Grimoire.Domain.Entity.Book.Segment;
using Application.Import;

public record struct FormattingState(
    bool IsBold = false,
    bool IsItalic = false,
    string? FootnoteId = null
);

public interface IInlineTagHandler {
    string[] SupportedTags { get; }
    void Handle(
        IElement element,
        List<SegmentModel> segments,
        List<TextRun> currentRuns,
        FormattingState state,
        List<TempImageReference>? imageRefs,
        IReadOnlyDictionary<string, byte[]> images,
        Action<INode, FormattingState> recurse);
}

public sealed class BrTagHandler : IInlineTagHandler {
    public string[] SupportedTags => ["br"];

    public void Handle(
        IElement element,
        List<SegmentModel> segments,
        List<TextRun> currentRuns,
        FormattingState state,
        List<TempImageReference>? imageRefs,
        IReadOnlyDictionary<string, byte[]> images,
        Action<INode, FormattingState> recurse) {

        if (currentRuns.Count > 0) {
            segments.Add(new TextSegmentModel { Runs = [.. currentRuns] });
            currentRuns.Clear();
        }
    }
}

public sealed class ImgTagHandler : IInlineTagHandler {
    public string[] SupportedTags => ["img"];

    public void Handle(
        IElement element,
        List<SegmentModel> segments,
        List<TextRun> currentRuns,
        FormattingState state,
        List<TempImageReference>? imageRefs,
        IReadOnlyDictionary<string, byte[]> images,
        Action<INode, FormattingState> recurse) {

        if (currentRuns.Count > 0) {
            segments.Add(new TextSegmentModel { Runs = [.. currentRuns] });
            currentRuns.Clear();
        }

        var src = element.GetAttribute("src");
        if (!string.IsNullOrEmpty(src)) {
            var normalizedSrc = NormalizeImagePath(src);
            imageRefs?.Add(new TempImageReference {
                SourceHref = normalizedSrc,
                AssetKey = images.ContainsKey(normalizedSrc) ? normalizedSrc : src
            });
            segments.Add(new ImageSegmentModel {
                AssetKey = images.ContainsKey(normalizedSrc) ? normalizedSrc : src,
                Caption = element.GetAttribute("alt")
            });
        }
    }

    private static string NormalizeImagePath(string src) {
        return src.Replace('\\', '/').Split('/')[^1];
    }
}

public sealed class EmphasisTagHandler : IInlineTagHandler {
    public string[] SupportedTags => ["em", "i"];

    public void Handle(
        IElement element,
        List<SegmentModel> segments,
        List<TextRun> currentRuns,
        FormattingState state,
        List<TempImageReference>? imageRefs,
        IReadOnlyDictionary<string, byte[]> images,
        Action<INode, FormattingState> recurse) {

        foreach (var childNode in element.ChildNodes) {
            recurse(childNode, state with { IsItalic = true });
        }
    }
}

public sealed class StrongTagHandler : IInlineTagHandler {
    public string[] SupportedTags => ["strong", "b"];

    public void Handle(
        IElement element,
        List<SegmentModel> segments,
        List<TextRun> currentRuns,
        FormattingState state,
        List<TempImageReference>? imageRefs,
        IReadOnlyDictionary<string, byte[]> images,
        Action<INode, FormattingState> recurse) {

        foreach (var childNode in element.ChildNodes) {
            recurse(childNode, state with { IsBold = true });
        }
    }
}

public sealed class AnchorTagHandler : IInlineTagHandler {
    public string[] SupportedTags => ["a"];

    public void Handle(
        IElement element,
        List<SegmentModel> segments,
        List<TextRun> currentRuns,
        FormattingState state,
        List<TempImageReference>? imageRefs,
        IReadOnlyDictionary<string, byte[]> images,
        Action<INode, FormattingState> recurse) {

        var href = element.GetAttribute("href") ?? string.Empty;
        var refType = element.GetAttribute("epub:type");
        string? nextFootnoteId = state.FootnoteId;
        if (refType == "noteref" && href.StartsWith('#')) {
            nextFootnoteId = href.TrimStart('#');
        }
        foreach (var childNode in element.ChildNodes) {
            recurse(childNode, state with { FootnoteId = nextFootnoteId });
        }
    }
}

public sealed class SpanTagHandler : IInlineTagHandler {
    public string[] SupportedTags => ["span", "sup"];

    public void Handle(
        IElement element,
        List<SegmentModel> segments,
        List<TextRun> currentRuns,
        FormattingState state,
        List<TempImageReference>? imageRefs,
        IReadOnlyDictionary<string, byte[]> images,
        Action<INode, FormattingState> recurse) {

        foreach (var childNode in element.ChildNodes) {
            recurse(childNode, state);
        }
    }
}
