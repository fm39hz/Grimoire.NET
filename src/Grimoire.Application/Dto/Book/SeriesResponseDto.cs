namespace Grimoire.Application.Dto.Book;

using System.Text.Json.Serialization;
using Metadata;

public class SeriesResponseDto {
	public string Title { get; init; } = string.Empty;
	public SeriesMetadataDto Metadata { get; init; } = new();
	public string Id { get; init; } = string.Empty;

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTime? CreatedAt { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTime? UpdatedAt { get; set; }
}
