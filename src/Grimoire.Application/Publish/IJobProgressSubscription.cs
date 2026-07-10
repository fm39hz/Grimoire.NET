namespace Grimoire.Application.Publish;

using Grimoire.Application.Publish.Dto;

public interface IJobProgressSubscription {
	public IAsyncEnumerable<PublishJobStatusDto> Subscribe(string jobId, CancellationToken cancellationToken);
}
