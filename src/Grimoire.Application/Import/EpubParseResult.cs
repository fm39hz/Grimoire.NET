namespace Grimoire.Application.Import;

using Domain.Entity.Book;
using Dto.Book;

public class EpubParseResult {
    public string Title { get; init; } = string.Empty;
    public string? Author { get; init; }
    public string? Description { get; init; }
    public byte[]? CoverBytes { get; init; }
    public string? CoverContentType { get; init; }
    public List<EpubVolumeEntry> Volumes { get; set; } = [];
    public Dictionary<string, string> ChapterHtmlMap { get; init; } = [];
    public Dictionary<string, byte[]> Images { get; init; } = [];
}

public class EpubVolumeEntry {
    public int Order { get; init; }
    public string Title { get; init; } = string.Empty;
    public List<EpubChapterEntry> Chapters { get; init; } = [];
}

public class EpubChapterEntry {
    public int Order { get; init; }
    public string Title { get; init; } = string.Empty;
    public string HtmlContent { get; init; } = string.Empty;
    public string? SourceHref { get; init; }
}

public class ParsedChapter {
    public List<SegmentModel> Segments { get; init; } = [];
    public List<ImportFootnoteDto> Footnotes { get; init; } = [];
    public List<TempImageReference> Images { get; init; } = [];
}

public class TempImageReference {
    public string SourceHref { get; init; } = string.Empty;
    public string AssetKey { get; set; } = string.Empty;
}
