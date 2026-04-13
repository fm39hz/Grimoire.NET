namespace Grimoire.Infrastructure.Export.Epub;

using Common;

public class PackageBuilderFactory(ITemplateEngine templateEngine) : IPackageBuilderFactory {
	public IPackageBuilder Create() => new EpubPackageBuilder(templateEngine);
}
