namespace Grimoire.Domain.Entity.Book;

using Segment;

/// <summary>
/// Represents a specific version or variant of a chapter.
/// </summary>
public record ChapterVariantModel : BaseModel {
    /// <summary>
    /// Foreign key to the chapter.
    /// </summary>
    public required Guid ChapterId { get; init; }

    /// <summary>
    /// Reference to the parent chapter.
    /// </summary>
    public ChapterModel? Chapter { get; init; }

    /// <summary>
    /// The type of this chapter variant.
    /// </summary>
    public required VariantType Type { get; set; }

    /// <summary>
    /// The language of this variant. Defaults to "vi-VN".
    /// </summary>
    public string Language { get; set; } = "vi-VN";

    /// <summary>
    /// Name of the translator or uploader.
    /// </summary>
    public string? SourceName { get; set; }

    /// <summary>
    /// The total word count of the chapter variant.
    /// </summary>
    public int WordCount { get; set; }

    /// <summary>
    /// Content of the chapter, composed of various segments.
    /// </summary>
    public List<SegmentModel> Content { get; set; } = [];

    /// <summary>
    /// A list of footnotes for this chapter variant.
    /// </summary>
    public List<FootnoteSegmentModel> Footnotes { get; set; } = [];
}
