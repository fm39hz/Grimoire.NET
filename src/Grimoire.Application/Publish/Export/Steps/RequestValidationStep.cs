namespace Grimoire.Application.Publish.Export.Steps;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Publish.Dto;
using FluentValidation;

public sealed class RequestValidationStep(IValidator<BinderyRequestDto> validator) : IExportPipelineStep {
	public int Order => 0;

	public async Task ExecuteAsync(ExportPipelineContext context, CancellationToken cancellationToken) {
		var validationResult = await validator.ValidateAsync(context.Request, cancellationToken);
		if (!validationResult.IsValid) {
			context.Result = JobResult.Fail("Validation failed: " + 
				string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
		}
	}
}
