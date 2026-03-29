namespace Grimoire.Application.Export;

using Domain.Entity.Book;

/// <summary>
///     A resolved image asset with a lazy stream provider.
///     Metadata is eager; the stream itself is only opened when the strategy requests it.
/// </summary>
public record ResolvedAsset(
	AssetModel Asset,
	Func<Task<Stream?>> StreamProvider
);
