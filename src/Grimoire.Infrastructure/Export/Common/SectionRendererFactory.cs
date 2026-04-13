namespace Grimoire.Infrastructure.Export.Common;

using Application.Service.Strategy;

public class SectionRendererFactory(IEnumerable<ISectionRenderer> renderers) : ISectionRendererFactory {
	public ISectionRenderer? Resolve(ExportFormat format) => renderers.FirstOrDefault(r => r.Format == format);
}
