namespace Grimoire.Infrastructure.Export.Common;

using Application.Service.Strategy;

public interface ISectionRendererFactory {
	public ISectionRenderer? Resolve(ExportFormat format);
}
