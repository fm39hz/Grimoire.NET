namespace Grimoire.Infrastructure.Research;

using System.Text.Json;
using Application.Ingestion.Research;

public sealed class GoogleBooksResearchProvider(ResearchHttpClient http) : IResearchProvider {
	public string Name => "google-books";

	public async Task<ResearchProviderResult> Search(ResearchQuery query, CancellationToken cancellationToken = default) {
		var parts = new List<string>();
		if (query.Titles.FirstOrDefault() is { Length: > 0 } title) parts.Add("intitle:" + title);
		if (query.Authors.FirstOrDefault() is { Length: > 0 } author) parts.Add("inauthor:" + author);
		foreach (var isbn in query.Isbns ?? []) parts.Add("isbn:" + isbn);
		var uri = "https://www.googleapis.com/books/v1/volumes?q=" + Uri.EscapeDataString(string.Join(' ', parts)) + "&maxResults=10";
		using var response = await http.Get(uri, cancellationToken);
		response.EnsureSuccessStatusCode();
		await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
		using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
		var candidates = new List<ResearchCandidateDto>();
		if (!document.RootElement.TryGetProperty("items", out var items)) return new ResearchProviderResult(Name, [], [], []);
		foreach (var item in items.EnumerateArray()) {
			var info = item.GetProperty("volumeInfo");
			var id = item.GetProperty("id").GetString() ?? string.Empty;
			var itemTitle = GetString(info, "title") ?? "Untitled";
			var authors = GetStrings(info, "authors");
			var isbns = new List<string>();
			if (info.TryGetProperty("industryIdentifiers", out var identifiers)) {
				isbns.AddRange(identifiers.EnumerateArray()
					.Where(static identifier => identifier.TryGetProperty("identifier", out _))
					.Select(static identifier => identifier.GetProperty("identifier").GetString()!)
					.Where(static value => !string.IsNullOrWhiteSpace(value)));
			}
			var published = GetString(info, "publishedDate");
			int? year = published is { Length: >= 4 } && int.TryParse(published[..4], out var parsedYear) ? parsedYear : null;
			var link = GetString(info, "infoLink");
			candidates.Add(new ResearchCandidateDto(Name, id, "bibliographic-volume", itemTitle, authors, isbns, year, link, 0.75));
		}
		var evidence = candidates.Take(3).SelectMany(candidate =>
			new[] { new ResearchEvidenceDto("title", candidate.Title, Name, candidate.Uri, candidate.Confidence, ObservedAt: DateTimeOffset.UtcNow) }
			.Concat(candidate.Authors.Select(author => new ResearchEvidenceDto("author", author, Name, candidate.Uri, candidate.Confidence, ObservedAt: DateTimeOffset.UtcNow)))
			.Concat(candidate.Isbns.Select(isbn => new ResearchEvidenceDto("isbn", isbn, Name, candidate.Uri, candidate.Confidence, ObservedAt: DateTimeOffset.UtcNow)))).ToList();
		return new ResearchProviderResult(Name, candidates, evidence, []);
	}

	private static string? GetString(JsonElement item, string name) =>
		item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

	private static List<string> GetStrings(JsonElement item, string name) =>
		item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
			? value.EnumerateArray().Select(static entry => entry.GetString()).Where(static entry => entry is not null).Cast<string>().ToList()
			: [];
}
