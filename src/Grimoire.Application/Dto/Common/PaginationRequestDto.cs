namespace Grimoire.Application.Dto.Common;

using System.ComponentModel;

public record PaginationRequestDto {
	[DefaultValue(1)] public int Page { get; set; } = 1;
	[DefaultValue(10)] public int PageSize { get; set; } = 10;
}
