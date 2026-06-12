namespace Grimoire.Infrastructure.Export.Common;

using Application.Export;

public interface IPackageBuilderFactory {
	/// <summary>
	///     Creates a fresh package builder instance.
	/// </summary>
	public IPackageBuilder Create();
}
