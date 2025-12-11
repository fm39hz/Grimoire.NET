namespace Grimoire.Domain.Entity;

/// <summary>
///     The base for every Entity in this project
/// </summary>
public abstract class BaseModel : IModel {
	/// <summary>
	///     Timestamp when the entity was created
	/// </summary>
	public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

	/// <summary>
	///     Timestamp when the entity was last updated
	/// </summary>
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	///     Unique identifier for the entity
	/// </summary>
	public Guid Id { get; init; } = Guid.CreateVersion7();

	/// <summary>
	///     Marks the entity as updated with current timestamp
	/// </summary>
	public virtual void MarkAsUpdated() => UpdatedAt = DateTime.UtcNow;
}
