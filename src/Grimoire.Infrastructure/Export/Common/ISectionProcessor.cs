namespace Grimoire.Infrastructure.Export.Common;

using Application.Dto.Book;

public interface ISectionProcessor<in TContext> where TContext : IBookSectionProcessorContext {
	public Task ProcessAsync(ExportSectionDto section, TContext context);
}
