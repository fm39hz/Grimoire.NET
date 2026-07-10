namespace Grimoire.Domain.Entity.Book;

using Microsoft.EntityFrameworkCore;
using Common.ValueObject;

/// <summary>
///     Represents a chapter within a volume
/// </summary>
public class ChapterModel : BaseModel {
	private LTree _dbPath = string.Empty;

	/// <summary>
	///     The hierarchical path for this chapter node in the domain layer.
	/// </summary>
	public BookPath Path {
		get => new(_dbPath.ToString());
		set => _dbPath = (LTree)value.Value;
	}

	/// <summary>
	///     The database-mapped LTree path. Used by EF Core and Repository queries.
	/// </summary>
	public LTree DbPath {
		get => _dbPath;
		set => _dbPath = value;
	}

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
