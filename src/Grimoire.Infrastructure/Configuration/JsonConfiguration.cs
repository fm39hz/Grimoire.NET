namespace Grimoire.Infrastructure.Configuration;

using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Entity.Book;
using Domain.Entity.Book.Metadata;
using Domain.Entity.Book.Segment;
using Microsoft.EntityFrameworkCore.ChangeTracking;

public static class JsonConfiguration {
	public static readonly JsonSerializerOptions JsonOptions = new() {
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		ReferenceHandler = ReferenceHandler.IgnoreCycles,
		AllowOutOfOrderMetadataProperties = true,
	};

	public static readonly ValueComparer<SeriesMetadata> MetadataComparer = new(
		(c1, c2) => JsonSerializer.Serialize(c1, JsonOptions) ==
					JsonSerializer.Serialize(c2, JsonOptions),
		c => JsonSerializer.Serialize(c, JsonOptions).GetHashCode(),
		c => JsonSerializer.Deserialize<SeriesMetadata>(
			JsonSerializer.Serialize(c, JsonOptions),
			JsonOptions)!
		);

	public static readonly ValueComparer<List<SegmentModel>> ContentComparer = new(
		(c1, c2) => JsonSerializer.Serialize(c1, JsonOptions) ==
					JsonSerializer.Serialize(c2, JsonOptions),
		c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
		c => JsonSerializer.Deserialize<List<SegmentModel>>(JsonSerializer.Serialize(c, JsonOptions),
			JsonOptions)!
		);

	public static readonly ValueComparer<List<FootnoteSegmentModel>> FootnoteComparer =
		new(
			(c1, c2) => JsonSerializer.Serialize(c1, JsonOptions) ==
						JsonSerializer.Serialize(c2, JsonOptions),
			c => JsonSerializer.Serialize(c, JsonOptions).GetHashCode(),
			c => JsonSerializer.Deserialize<List<FootnoteSegmentModel>>(
				JsonSerializer.Serialize(c, JsonOptions),
				JsonOptions)!
			);
}
