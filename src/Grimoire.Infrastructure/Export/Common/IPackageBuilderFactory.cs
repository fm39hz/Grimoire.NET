namespace Grimoire.Infrastructure.Export.Common;

public interface IPackageBuilderFactory {
	/// <summary>
	///     Creates a fresh package builder instance.
	/// </summary>
	public IPackageBuilder Create();
}
