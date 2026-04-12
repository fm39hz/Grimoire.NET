namespace Grimoire.Infrastructure.Export.Epub;

using Common;

public class EpubPackageBuilderFactory(ITemplateEngine templateEngine) : IEpubPackageBuilderFactory {
	public EpubPackageBuilder Create() => new(templateEngine);
}
