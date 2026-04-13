namespace Grimoire.Infrastructure.Export.Epub;

/// <summary>
///     Represents a resource (file) in an EPUB package
/// </summary>
public class EpubResource {
	public required string Path { get; init; }
	public EpubResourceType Type { get; init; }
	public string? TextContent { get; init; }
	public byte[]? BinaryContent { get; init; }
	public Func<Task<Stream?>>? StreamProvider { get; init; }

	/// <summary>
	///     Creates a text resource (HTML, CSS, XML)
	/// </summary>
	public static EpubResource FromText(string path, string content) => new() {
		Path = path,
		Type = EpubResourceType.Text,
		TextContent = content
	};

	/// <summary>
	///     Creates a binary resource from byte array
	/// </summary>
	public static EpubResource FromBytes(string path, byte[] content) => new() {
		Path = path,
		Type = EpubResourceType.Binary,
		BinaryContent = content
	};

	/// <summary>
	///     Creates a binary resource from stream provider (lazy loading)
	/// </summary>
	public static EpubResource FromStream(string path, Func<Task<Stream?>> streamProvider) => new() {
		Path = path,
		Type = EpubResourceType.Stream,
		StreamProvider = streamProvider
	};
}

/// <summary>
///     Type of EPUB resource
/// </summary>
public enum EpubResourceType {
	Text,
	Binary,
	Stream
}
