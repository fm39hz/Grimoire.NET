namespace Grimoire.Application.Service.Pipeline.Publishing.Steps;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Service.Strategy;

public sealed class SerializationStep(IEnumerable<IExportStrategy> strategies) : IPublishingPipelineStep {
	public int ExecutionOrder => 20; // Runs after BuildContextStep (10)

	public async Task ExecuteAsync(PublishingContext context, CancellationToken cancellationToken) {
		if (context.ExportContext == null) {
			throw new InvalidOperationException("ExportContext has not been built yet.");
		}

		var strategy = strategies.FirstOrDefault(s => s.Format == context.Format) ??
			throw new InvalidOperationException($"No export strategy found for format {context.Format}");

		context.Result = await strategy.ExportAsync(context.ExportContext, cancellationToken);
	}
}
