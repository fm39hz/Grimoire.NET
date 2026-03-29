namespace Grimoire.Infrastructure.Export.Common;

using Grimoire.Application.Export;
using Grimoire.Application.Service.Strategy;
using Microsoft.Extensions.DependencyInjection;

public class SectionProcessorFactory<TContext>(
	IServiceProvider serviceProvider,
	ExportFormat format
	) : ISectionProcessorFactory<TContext> where TContext : IBookSectionProcessorContext {
	private readonly IServiceProvider _serviceProvider = serviceProvider;
	private readonly ExportFormat _format = format;

	public ISectionProcessor<TContext>? GetProcessor(BookSection sectionType) {
		var key = $"{_format}:{sectionType.ToString().ToLowerInvariant()}";
		return _serviceProvider.GetKeyedService<ISectionProcessor<TContext>>(key);
	}
}
