namespace Grimoire.Application.Service.Contract;

public interface ISeriesRevisionService {
	IDisposable Suppress();
	Task AdvanceAsync(Guid seriesId, CancellationToken cancellationToken = default);
}
