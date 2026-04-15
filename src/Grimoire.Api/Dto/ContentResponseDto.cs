namespace Grimoire.Api.Dto;

public record ContentResponseDto {
	public string Content { get; init; } = string.Empty;
	public string ContentType { get; init; } = "text/markdown";
}
