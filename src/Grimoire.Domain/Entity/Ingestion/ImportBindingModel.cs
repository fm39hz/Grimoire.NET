namespace Grimoire.Domain.Entity.Ingestion;

using Book;

public sealed class ImportBindingModel : BaseModel {
	public Guid ImportSourceId { get; init; }
	public required string ExternalNodeKey { get; init; }
	public Guid TargetNodeId { get; set; }
	public BookNodeType TargetNodeType { get; set; }
	public required string Role { get; set; }
	public string? LastImportedHash { get; set; }
	public string? BaseSnapshotKey { get; set; }
	public bool Pinned { get; set; }
	public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
}
