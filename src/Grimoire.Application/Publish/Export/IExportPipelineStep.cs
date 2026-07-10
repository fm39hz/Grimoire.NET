namespace Grimoire.Application.Publish.Export;

using System.Threading;
using System.Threading.Tasks;

public interface IExportPipelineStep {
	public int Order { get; }
	public Task ExecuteAsync(ExportPipelineContext context, CancellationToken cancellationToken);
}
