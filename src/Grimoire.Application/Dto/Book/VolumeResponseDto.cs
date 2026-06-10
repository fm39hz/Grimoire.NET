namespace Grimoire.Application.Dto.Book;

using System.Text.Json.Serialization;
using Common;
using Domain.Entity.Book.Metadata;

public class VolumeResponseDto : ITimestampedDto {
	public string SeriesId { get; init; } = string.Empty;
	public int Order { get; init; }
	public string Title { get; init; } = string.Empty;
	public VolumeMetadata? Metadata { get; init; }
	public string Id { get; init; } = string.Empty;

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTime? CreatedAt { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTime? UpdatedAt { get; set; }
}
