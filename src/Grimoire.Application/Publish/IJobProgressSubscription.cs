namespace Grimoire.Application.Publish;

using Grimoire.Application.Publish.Dto;

public interface IJobProgressSubscription
{
    IAsyncEnumerable<PublishJobStatusDto> Subscribe(string jobId, CancellationToken cancellationToken);
}
