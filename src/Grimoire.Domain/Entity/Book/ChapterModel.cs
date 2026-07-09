namespace Grimoire.Domain.Entity.Book;

using Microsoft.EntityFrameworkCore;

/// <summary>
///     Represents a chapter within a volume
/// </summary>
public class ChapterModel : BaseModel {
	/// <summary>
	///     The hierarchical ltree path for this chapter node.
	///     Using Microsoft.EntityFrameworkCore.LTree directly in Domain (pragmatic design)
	///     to leverage native PG LTree operations (LCA, IsDescendantOf, Subpath) both in SQL LINQ
	///     and in-memory tests, avoiding custom C# string parsing workarounds.
	/// </summary>
	public LTree Path { get; set; } = string.Empty;

	/// <summary>
	///     Order of this chapter within the volume
	/// </summary>
	public double Order { get; set; }


	/// <summary>
	///     Title of the chapter
	/// </summary>
	public required string Title { get; set; }

	/// <summary>
	///     Status of the chapter
	/// </summary>
	public ChapterStatus Status { get; set; } = ChapterStatus.Draft;
}
