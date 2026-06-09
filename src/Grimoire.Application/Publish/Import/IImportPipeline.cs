namespace Grimoire.Application.Publish.Import;

using System.Threading;
using System.Threading.Tasks;

public interface IImportPipeline
{
    Task ExecuteAsync(ImportPipelineContext context, CancellationToken cancellationToken);
}
