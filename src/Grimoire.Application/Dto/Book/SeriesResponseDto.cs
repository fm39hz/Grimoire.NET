namespace Grimoire.Application.Dto.Book;

using Metadata;
using System.Text.Json.Serialization;

public class SeriesResponseDto {
	public string Title { get; init; } = string.Empty;
	public SeriesMetadataDto Metadata { get; init; } = new();
	public string Id { get; init; } = string.Empty;
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTime? CreatedAt { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTime? UpdatedAt { get; set; }
	/// <summary>
	///     Markdown representation of the series description.
	///     Only populated when ?markdown=true query parameter is used.
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Markdown { get; set; }
}
