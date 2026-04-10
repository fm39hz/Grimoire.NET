namespace Grimoire.Infrastructure.Export.Common;

using Application.Export;

public interface ISectionProcessorFactory<TContext> where TContext : IBookSectionProcessorContext {
	public ISectionProcessor<TContext>? GetProcessor(BookSection sectionType);
}
