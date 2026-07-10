namespace Grimoire.Application.Publish.Import;

using System.Threading;
using System.Threading.Tasks;

public interface IImportPipelineStep {
	public int Order { get; }
	public Task ExecuteAsync(ImportPipelineContext context, CancellationToken cancellationToken);
}
