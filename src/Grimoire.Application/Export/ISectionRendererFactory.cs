namespace Grimoire.Application.Export;

using Service.Strategy;

public interface ISectionRendererFactory {
	public ISectionRenderer? Resolve(ExportFormat format);
}
