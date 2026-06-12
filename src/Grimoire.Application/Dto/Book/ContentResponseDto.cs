namespace Grimoire.Application.Dto.Book;

using System.Collections.Generic;

public record ContentResponseDto {
	public string Data { get; init; } = string.Empty;
	public string Type { get; init; } = "text/markdown";
	public IReadOnlyList<AssetListingDto> Assets { get; init; } = [];
}
