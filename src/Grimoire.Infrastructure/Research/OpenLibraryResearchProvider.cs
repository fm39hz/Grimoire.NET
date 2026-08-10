namespace Grimoire.Infrastructure.Research;

using System.Text.Json;
using Application.Ingestion.Research;

public sealed class OpenLibraryResearchProvider(ResearchHttpClient http) : IResearchProvider {
	public string Name => "open-library";

	public async Task<ResearchProviderResult> Search(ResearchQuery query, CancellationToken cancellationToken = default) {
		var title = query.Titles.FirstOrDefault() ?? string.Empty;
		var author = query.Authors.FirstOrDefault();
		var uri = "https://openlibrary.org/search.json?title=" + Uri.EscapeDataString(title) +
			(string.IsNullOrWhiteSpace(author) ? string.Empty : "&author=" + Uri.EscapeDataString(author)) +
			"&fields=key,title,author_name,isbn,first_publish_year&limit=10";
		using var response = await http.Get(uri, cancellationToken);
		response.EnsureSuccessStatusCode();
		await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
		using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
		var candidates = new List<ResearchCandidateDto>();
		foreach (var item in document.RootElement.GetProperty("docs").EnumerateArray()) {
			var key = String(item, "key") ?? string.Empty;
			var itemTitle = String(item, "title") ?? "Untitled";
			var authors = Strings(item, "author_name");
			var isbns = Strings(item, "isbn").Take(20).ToList();
			int? year = item.TryGetProperty("first_publish_year", out var yearElement) && yearElement.TryGetInt32(out var parsedYear)
				? parsedYear : null;
			candidates.Add(new ResearchCandidateDto(Name, key, "bibliographic-work", itemTitle, authors, isbns, year,
				string.IsNullOrWhiteSpace(key) ? null : "https://openlibrary.org" + key, 0.8));
		}
		return new ResearchProviderResult(Name, candidates, ToEvidence(candidates), []);
	}

	private static IReadOnlyList<ResearchEvidenceDto> ToEvidence(IEnumerable<ResearchCandidateDto> candidates) =>
		candidates.Take(3).SelectMany(candidate =>
			new[] { new ResearchEvidenceDto("title", candidate.Title, candidate.Provider, candidate.Uri, candidate.Confidence, ObservedAt: DateTimeOffset.UtcNow) }
			.Concat(candidate.Authors.Select(author => new ResearchEvidenceDto("author", author, candidate.Provider, candidate.Uri, candidate.Confidence, ObservedAt: DateTimeOffset.UtcNow)))
			.Concat(candidate.Isbns.Take(5).Select(isbn => new ResearchEvidenceDto("isbn", isbn, candidate.Provider, candidate.Uri, candidate.Confidence, ObservedAt: DateTimeOffset.UtcNow))))
			.ToList();

	private static string? String(JsonElement item, string property) =>
		item.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

	private static List<string> Strings(JsonElement item, string property) =>
		item.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array
			? value.EnumerateArray().Where(static entry => entry.ValueKind == JsonValueKind.String).Select(static entry => entry.GetString()!).ToList()
			: [];
}
