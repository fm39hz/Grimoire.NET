namespace Grimoire.Domain.Entity.Ingestion;

public sealed class ImportSourceModel : BaseModel {
	public required string ProducerId { get; init; }
	public required string ExternalKey { get; init; }
	public required string Provider { get; set; }
	public string? Uri { get; set; }
	public Guid? TargetSeriesId { get; set; }
	public string? LastPackageHash { get; set; }
	public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
	public string MetadataJson { get; set; } = "{}";
}
