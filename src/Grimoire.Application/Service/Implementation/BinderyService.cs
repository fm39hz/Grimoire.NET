namespace Grimoire.Application.Service.Implementation;

using System.Threading;
using System.Threading.Tasks;
using Contract;
using Dto.Book;
using Grimoire.Application.Export;
using Pipeline.Publishing;
using Strategy;

public sealed class BinderyService(PublishingCoordinator publishingCoordinator) : IBinderyService {
	public async Task<ExportResult> ExportSeriesAsync(Guid seriesId, BinderyRequestDto request, CancellationToken cancellationToken = default) {
		var structure = request.Structure ?? ExportStructureDefaults.Standard();
		var context = new PublishingContext(seriesId, structure, request.Format);

		return await publishingCoordinator.ExecuteAsync(context, cancellationToken);
	}
}
