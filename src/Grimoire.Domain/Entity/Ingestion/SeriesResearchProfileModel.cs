namespace Grimoire.Domain.Entity.Ingestion;

public sealed class SeriesResearchProfileModel : BaseModel {
	public Guid SeriesId { get; init; }
	public long Revision { get; set; }
	public string ProfileJson { get; set; } = "{}";
}
