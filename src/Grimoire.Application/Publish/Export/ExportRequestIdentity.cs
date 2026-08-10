namespace Grimoire.Application.Publish.Export;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Grimoire.Application.Dto.Book;

public static class ExportRequestIdentity {
	public static string Create(BinderyRequestDto request) {
		var normalized = new {
			mode = request.Mode.Trim().ToLowerInvariant(),
			targets = request.TargetVolumeIds?.Order(StringComparer.Ordinal).ToArray(),
			groups = request.Groups?.Select(group => new {
				name = group.Name.Trim(),
				targets = group.TargetVolumeIds.Order(StringComparer.Ordinal).ToArray()
			}).OrderBy(static group => group.name, StringComparer.OrdinalIgnoreCase).ToArray(),
			structure = request.Structure
		};
		var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(normalized))))[..16].ToLowerInvariant();
		return $"{request.Format.ToString().ToLowerInvariant()}:{normalized.mode}:{hash}";
	}
}
