namespace Grimoire.Infrastructure.Persistence.Database;

using System.Text.Json;
using System.Text.Json.Serialization;

public static class JsonOptions {
	public static readonly JsonSerializerOptions Default = new() {
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};
}
