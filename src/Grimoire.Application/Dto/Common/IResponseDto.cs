namespace Grimoire.Application.Dto.Common;

using System.Text.Json.Serialization;

public interface IResponseDto {
	[JsonPropertyOrder(-1)]
	public Guid Id { get; init; }
}
