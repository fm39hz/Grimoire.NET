namespace Grimoire.Infrastructure.Export.Common;

using System;
using Application.Service.Strategy;
using Domain.Exception;

public static class SectionRendererFactoryExtensions {
	public static ISectionRenderer ResolveAndValidate(this ISectionRendererFactory factory, string format, out ExportFormat exportFormat) {
		if (!Enum.TryParse<ExportFormat>(format, true, out exportFormat)) {
			throw new ArgumentException($"Unsupported format: {format}");
		}

		if (exportFormat is not (ExportFormat.Markdown or ExportFormat.Html)) {
			throw new ArgumentException($"Content format must be 'markdown' or 'html', got: {format}");
		}

		return factory.Resolve(exportFormat) ?? throw new UnsupportedOperationException($"Renderer for format {format} is not implemented");
	}
}
