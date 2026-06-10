namespace Grimoire.Api.Extension;

using Grimoire.Application.Dto.Common;

public static class DtoExtension {
	public static T ApplyTimestampOption<T>(this T dto, bool? timestamp) where T : ITimestampedDto {
		if (timestamp != true) {
			dto.CreatedAt = null;
			dto.UpdatedAt = null;
		}
		return dto;
	}
}
