namespace Grimoire.Application.Publish.Import;

using System.Threading;
using System.Threading.Tasks;

public interface IImportPipelineStep
{
    int Order { get; }
    Task ExecuteAsync(ImportPipelineContext context, CancellationToken cancellationToken);
}
