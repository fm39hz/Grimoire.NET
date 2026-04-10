namespace Grimoire.Infrastructure.Export.Common;

using Application.Export;
using Application.Service.Strategy;
using Microsoft.Extensions.DependencyInjection;

public class SectionProcessorFactory<TContext>(
	IServiceProvider serviceProvider,
	ExportFormat format
	) : ISectionProcessorFactory<TContext> where TContext : IBookSectionProcessorContext {
	private readonly ExportFormat _format = format;
	private readonly IServiceProvider _serviceProvider = serviceProvider;

	public ISectionProcessor<TContext>? GetProcessor(BookSection sectionType) {
		var key = $"{_format}:{sectionType.ToString().ToLowerInvariant()}";
		return _serviceProvider.GetKeyedService<ISectionProcessor<TContext>>(key);
	}
}
