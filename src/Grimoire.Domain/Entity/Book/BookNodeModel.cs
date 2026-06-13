namespace Grimoire.Domain.Entity.Book;

/// <summary>
///     Canonical hierarchy node for book structure. Payload remains in the type-specific tables.
/// </summary>
public class BookNodeModel : BaseModel {
	public required BookNodeType Type { get; init; }
	public Guid? ParentId { get; set; }
	public double Order { get; set; }
	public required string Title { get; set; }
	public string Path { get; set; } = string.Empty;

	public static string CalculatePath(Guid id, string? parentPath) {
		var segment = "n" + id.ToString("N");
		return string.IsNullOrEmpty(parentPath) ? segment : $"{parentPath}.{segment}";
	}
}

