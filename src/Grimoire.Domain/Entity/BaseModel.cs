namespace Grimoire.Domain.Entity;

using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
///     The base for every Entity in this project
/// </summary>
public abstract record BaseModel : IModel {
	protected BaseModel(BaseModel model) {
		Id = model.Id;
		CreatedAt = model.CreatedAt;
		UpdatedAt = new DateTime();
	}

	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public Guid Id { get; init; } = Guid.CreateVersion7();

	public DateTime CreatedAt { get; init; }
	public DateTime UpdatedAt { get; set; }
}
