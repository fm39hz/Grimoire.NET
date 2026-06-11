namespace Grimoire.Application.Import;

using Domain.Entity.Book;
using Dto.Book;

public record NormalizedImport {
    public string Title { get; init; } = string.Empty;
    public string? Author { get; init; }
    public string? Description { get; init; }
    public NormalizedFile? Cover { get; init; }
    public List<string> Tags { get; init; } = [];
    public List<NormalizedVolume> Volumes { get; init; } = [];
    public List<NormalizedFile> Files { get; init; } = [];
}

public record NormalizedVolume {
    public int Order { get; init; }
    public string Title { get; init; } = string.Empty;
    public List<NormalizedChapter> Chapters { get; init; } = [];
}

public record NormalizedChapter {
    public int Order { get; init; }
    public string Title { get; init; } = string.Empty;
    public List<SegmentModel> Segments { get; init; } = [];
    public List<ImportFootnoteDto> Footnotes { get; init; } = [];
    public string? RawHtml { get; init; }
}

public record NormalizedFile {
    public string FileName { get; init; } = string.Empty;
    public byte[] Content { get; init; } = [];
    public string ContentType { get; init; } = "application/octet-stream";
}
