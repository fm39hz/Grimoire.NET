namespace Grimoire.Infrastructure.Export.Epub;

/// <summary>
///     Factory for creating new instances of EpubPackageBuilder
/// </summary>
public interface IEpubPackageBuilderFactory {
	/// <summary>
	///     Creates a new instance of EpubPackageBuilder
	/// </summary>
	/// <returns>A fresh EpubPackageBuilder instance</returns>
	public EpubPackageBuilder Create();
}
