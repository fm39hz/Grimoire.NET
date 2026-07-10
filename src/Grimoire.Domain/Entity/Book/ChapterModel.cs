namespace Grimoire.Domain.Entity.Book;

using Common.ValueObject;
using Microsoft.EntityFrameworkCore;

/// <summary>
///     Represents a chapter within a volume
/// </summary>
public class ChapterModel : BaseModel {

	/// <summary>
	///     The hierarchical path for this chapter node in the domain layer.
	/// </summary>
	public BookPath Path {
		get => new(DbPath.ToString());
		set => DbPath = (LTree)value.Value;
	}

	/// <summary>
	///     The database-mapped LTree path. Used by EF Core and Repository queries.
	/// </summary>
	public LTree DbPath { get; set; } = string.Empty;

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
