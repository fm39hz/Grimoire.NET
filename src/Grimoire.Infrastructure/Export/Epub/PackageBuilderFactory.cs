namespace Grimoire.Infrastructure.Export.Epub;

using Common;
using Grimoire.Application.Export;

public class PackageBuilderFactory(ITemplateEngine templateEngine) : IPackageBuilderFactory {
	public IPackageBuilder Create() => new EpubPackageBuilder(templateEngine);
}
