namespace Grimoire.Infrastructure.Export.Common;

using Domain.Entity.Book;

/// <summary>
///     Format-agnostic interface for building an ebook package.
///     Implementations own all path/file-layout decisions internally.
/// </summary>
public interface IPackageBuilder {
	/// <summary>
	///     Sets top-level book metadata.
	/// </summary>
	public void SetMetadata(BookPackageMetadata metadata);

	/// <summary>
	///     Registers an asset (image) by its already-resolved filename.
	///     The caller is responsible for filename resolution (e.g. "cover.jpg",
	///     "abc123.png"); the builder decides where to store it internally.
	/// </summary>
	/// <param name="resolvedFileName">Filename only — no path prefix.</param>
	/// <param name="streamProvider">Lazy stream factory.</param>
	/// <param name="refType">Cover or Content — drives format-specific treatment.</param>
	public void AddAsset(string resolvedFileName, Func<Task<Stream?>> streamProvider, AssetRefType refType);

	/// <summary>
	///     Adds an HTML content page identified by a stable logical ID.
	///     For <see cref="PageRole.TableOfContents"/> pass an empty string for
	///     htmlContent — the builder generates the navigation document itself.
	/// </summary>
	/// <returns>The resolved filename (e.g. "chapter_001.xhtml")</returns>
	public string AddPage(string pageId, string htmlContent, PageRole role = PageRole.Chapter);

	/// <summary>
	///     Sets the global stylesheet. Implementations decide whether to embed,
	///     link, or inline.
	/// </summary>
	public void AddStylesheet(string css);

	/// <summary>
	///     Sets the full navigation tree once all pages have been registered.
	///     PageId values in the tree must match those passed to <see cref="AddPage"/>.
	/// </summary>
	public void SetNavigation(IReadOnlyList<NavEntry> navEntries);

	/// <summary>
	///     Builds and returns the final package stream.
	/// </summary>
	public Task<Stream> BuildAsync();
}
