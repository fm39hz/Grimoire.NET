namespace Grimoire.Infrastructure.Research;

public sealed class ResearchHttpClient : IDisposable {
	private readonly HttpClient client = new() {
		Timeout = TimeSpan.FromSeconds(15)
	};

	public Task<HttpResponseMessage> Get(string uri, CancellationToken cancellationToken) =>
		client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

	public void Dispose() => client.Dispose();
}
