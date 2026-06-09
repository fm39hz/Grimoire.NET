namespace Grimoire.Domain.Entity.Book;

/// <summary>
///     Canonical hierarchy node for book structure. Payload remains in the type-specific tables.
/// </summary>
public class BookNodeModel : BaseModel {
	public required BookNodeType Type { get; init; }
	public Guid? ParentId { get; set; }
	public float Order { get; set; }
	public required string Title { get; set; }
}
