namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common.Repository;
using Domain.Exception;

/// <summary>
/// Advances the optimistic revision at public mutation boundaries. Multi-step import flows suppress
/// nested advances and perform one explicit advance when their transaction has succeeded.
/// </summary>
public sealed class SeriesRevisionService(ISeriesRepository seriesRepository) : ISeriesRevisionService {
	private int _suppressionDepth;

	public IDisposable Suppress() {
		_suppressionDepth++;
		return new Suppression(this);
	}

	public async Task AdvanceAsync(Guid seriesId, CancellationToken cancellationToken = default) {
		if (_suppressionDepth > 0) return;

		var series = await seriesRepository.FindOneTracked(seriesId, cancellationToken)
			?? throw new EntityNotFoundException($"Series with id {seriesId} not found");
		series.AdvanceRevision(series.Revision);
		await seriesRepository.Update(series, cancellationToken);
	}

	private sealed class Suppression(SeriesRevisionService owner) : IDisposable {
		private SeriesRevisionService? _owner = owner;

		public void Dispose() {
			var current = Interlocked.Exchange(ref _owner, null);
			if (current is not null) current._suppressionDepth--;
		}
	}
}
