namespace Grimoire.Application.Import;

using Domain.Common;
using Domain.Common.Repository;
using Dto.Book;
using Mapper;

public sealed record ResolvedVolume {
    public required Guid Id { get; init; }
    public int VolumeOrder { get; init; }
    public bool WasCreated { get; init; }
    public required List<ResolvedChapter> Chapters { get; init; }
}

public sealed record ResolvedChapter {
    public required int Order { get; init; }
    public required string Title { get; init; }
}

public interface IVolumeTreeResolver {
    Task<List<ResolvedVolume>> ResolveAsync(
        Guid seriesId,
        List<NormalizedVolume> volumes,
        CancellationToken cancellationToken = default);
}

public sealed class VolumeTreeResolver(
    IVolumeRepository volumeRepository,
    IBookMapper mapper) : IVolumeTreeResolver {

    public async Task<List<ResolvedVolume>> ResolveAsync(
        Guid seriesId,
        List<NormalizedVolume> volumes,
        CancellationToken cancellationToken = default) {

        var existing = (await volumeRepository.FindBySeriesId(seriesId, cancellationToken))
            .ToDictionary(v => v.Order);

        var result = new List<ResolvedVolume>();

        foreach (var entry in volumes)
        {
            if (existing.TryGetValue(entry.Order, out var vol))
            {
                vol.Title = entry.Title;
                await volumeRepository.Update(vol, cancellationToken);
                result.Add(new ResolvedVolume
                {
                    Id = vol.Id,
                    VolumeOrder = entry.Order,
                    WasCreated = false,
                    Chapters = entry.Chapters.Select(c => new ResolvedChapter
                    {
                        Order = c.Order,
                        Title = c.Title
                    }).ToList()
                });
            }
            else
            {
                var newVol = mapper.CreateVolume(
                    new CreateVolumeRequestDto(
                        PrefixedId.ToString(EntityPrefix.Series, seriesId),
                        entry.Order,
                        entry.Title,
                        null),
                    seriesId);
                var created = await volumeRepository.Create(newVol, cancellationToken);
                result.Add(new ResolvedVolume
                {
                    Id = created.Id,
                    VolumeOrder = entry.Order,
                    WasCreated = true,
                    Chapters = entry.Chapters.Select(c => new ResolvedChapter
                    {
                        Order = c.Order,
                        Title = c.Title
                    }).ToList()
                });
            }
        }

        return result;
    }
}
