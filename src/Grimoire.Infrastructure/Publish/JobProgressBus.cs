namespace Grimoire.Infrastructure.Publish;

using System.Collections.Concurrent;
using System.Threading.Channels;
using Grimoire.Application.Publish;
using Grimoire.Application.Publish.Dto;

public sealed class JobProgressBus : IJobProgressTracker, IJobProgressSubscription
{
    private readonly ConcurrentDictionary<string, List<ChannelWriter<PublishJobStatusDto>>> _subscribers = new();

    public void UpdateProgress(string jobId, int progress, string? stage = null)
    {
        var status = new PublishJobStatusDto(jobId, "Processing", Progress: progress, Stage: stage);
        NotifySubscribers(jobId, status);
    }

    public void CompleteJob(string jobId, string? downloadUrl = null)
    {
        var status = new PublishJobStatusDto(jobId, "Completed", DownloadUrl: downloadUrl);
        NotifySubscribers(jobId, status);
        CompleteChannels(jobId);
    }

    public void FailJob(string jobId, string errorMessage)
    {
        var status = new PublishJobStatusDto(jobId, "Failed", Error: errorMessage);
        NotifySubscribers(jobId, status);
        CompleteChannels(jobId);
    }

    public IAsyncEnumerable<PublishJobStatusDto> Subscribe(string jobId, CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<PublishJobStatusDto>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _subscribers.AddOrUpdate(
            jobId,
            _ => new List<ChannelWriter<PublishJobStatusDto>> { channel.Writer },
            (_, list) =>
            {
                lock (list)
                {
                    list.Add(channel.Writer);
                }
                return list;
            });

        cancellationToken.Register(() =>
        {
            if (_subscribers.TryGetValue(jobId, out var list))
            {
                lock (list)
                {
                    list.Remove(channel.Writer);
                    if (list.Count == 0)
                    {
                        _subscribers.TryRemove(jobId, out _);
                    }
                }
            }
            channel.Writer.TryComplete();
        });

        return channel.Reader.ReadAllAsync(cancellationToken);
    }

    private void NotifySubscribers(string jobId, PublishJobStatusDto status)
    {
        if (_subscribers.TryGetValue(jobId, out var list))
        {
            lock (list)
            {
                foreach (var writer in list)
                {
                    writer.TryWrite(status);
                }
            }
        }
    }

    private void CompleteChannels(string jobId)
    {
        if (_subscribers.TryRemove(jobId, out var list))
        {
            lock (list)
            {
                foreach (var writer in list)
                {
                    writer.TryComplete();
                }
            }
        }
    }
}
