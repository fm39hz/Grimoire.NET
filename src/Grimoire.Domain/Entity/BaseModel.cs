namespace Grimoire.Domain.Entity;

/// <summary>
///     The base for every Entity in this project
/// </summary>
public abstract record BaseModel : IModel {
	protected BaseModel(BaseModel model) {
		Id = model.Id;
		CreatedAt = model.CreatedAt;
		UpdatedAt = DateTime.UtcNow;
	}

	public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	public Guid Id { get; init; } = Guid.CreateVersion7();
}
