namespace Grimoire.Application.Common;

/// <summary>
///     Helper for converting between Guid and prefixed ID strings at API boundary
/// </summary>
public static class PrefixedId {
	private const char Separator = '_';

	/// <summary>
	///     Convert Guid to prefixed string (e.g., ser_<uuid>)
	/// </summary>
	public static string ToString(string prefix, Guid guid) {
		return $"{prefix}{Separator}{guid}";
	}

	/// <summary>
	///     Parse prefixed ID string and extract the Guid
	/// </summary>
	public static Guid ToGuid(string prefixedId) {
		if (string.IsNullOrWhiteSpace(prefixedId))
			throw new ArgumentException("ID cannot be null or empty", nameof(prefixedId));

		var parts = prefixedId.Split(Separator, 2);
		if (parts.Length != 2)
			throw new FormatException($"Invalid prefixed ID format: {prefixedId}");

		if (!Guid.TryParse(parts[1], out var guid))
			throw new FormatException($"Invalid GUID in prefixed ID: {prefixedId}");

		return guid;
	}

	/// <summary>
	///     Parse prefixed ID string with validation of expected prefix
	/// </summary>
	public static Guid ToGuid(string prefixedId, string expectedPrefix) {
		if (string.IsNullOrWhiteSpace(prefixedId))
			throw new ArgumentException("ID cannot be null or empty", nameof(prefixedId));

		var parts = prefixedId.Split(Separator, 2);
		if (parts.Length != 2)
			throw new FormatException($"Invalid prefixed ID format: {prefixedId}");

		var actualPrefix = parts[0];
		if (actualPrefix != expectedPrefix)
			throw new ArgumentException(
				$"Invalid ID prefix. Expected '{expectedPrefix}{Separator}' but got '{actualPrefix}{Separator}'",
				nameof(prefixedId));

		if (!Guid.TryParse(parts[1], out var guid))
			throw new FormatException($"Invalid GUID in prefixed ID: {prefixedId}");

		return guid;
	}

	/// <summary>
	///     Try to parse prefixed ID string
	/// </summary>
	public static bool TryToGuid(string? prefixedId, out Guid guid) {
		guid = default;
		if (string.IsNullOrWhiteSpace(prefixedId))
			return false;

		var parts = prefixedId.Split(Separator, 2);
		if (parts.Length != 2)
			return false;

		return Guid.TryParse(parts[1], out guid);
	}

	/// <summary>
	///     Try to parse prefixed ID string with prefix validation
	/// </summary>
	public static bool TryToGuid(string? prefixedId, string expectedPrefix, out Guid guid) {
		guid = default;
		if (string.IsNullOrWhiteSpace(prefixedId))
			return false;

		var parts = prefixedId.Split(Separator, 2);
		if (parts.Length != 2)
			return false;

		if (parts[0] != expectedPrefix)
			return false;

		return Guid.TryParse(parts[1], out guid);
	}

	/// <summary>
	///     Get the prefix from a prefixed ID
	/// </summary>
	public static string? GetPrefix(string? prefixedId) {
		if (string.IsNullOrWhiteSpace(prefixedId))
			return null;

		var separatorIndex = prefixedId.IndexOf(Separator);
		if (separatorIndex <= 0)
			return null;

		return prefixedId[..separatorIndex];
	}
}
