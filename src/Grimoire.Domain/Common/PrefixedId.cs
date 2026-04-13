namespace Grimoire.Domain.Common;

/// <summary>
///     Helper for converting between Guid and prefixed ID strings.
///     This is a domain concept for entity identification.
/// </summary>
public static class PrefixedId {
	private const char SEPARATOR = '_';

	/// <summary>
	///     Convert Guid to prefixed string (e.g., ser_<uuid>)
	/// </summary>
	public static string ToString(string prefix, Guid id) => $"{prefix}{SEPARATOR}{id}";

	/// <summary>
	///     Parse prefixed ID string and extract the Guid
	/// </summary>
	public static Guid ToGuid(string prefixedId) {
		if (string.IsNullOrWhiteSpace(prefixedId)) {
			throw new ArgumentException("ID cannot be null or empty", nameof(prefixedId));
		}

		var parts = prefixedId.Split(SEPARATOR, 2);
		return parts.Length != 2
			? throw new FormatException($"Invalid prefixed ID format: {prefixedId}")
			: !Guid.TryParse(parts[1], out var guid)
				? throw new FormatException($"Invalid GUID in prefixed ID: {prefixedId}")
				: guid;
	}

	/// <summary>
	///     Parse prefixed ID string with validation of expected prefix
	/// </summary>
	public static Guid ToGuid(string prefixedId, string expectedPrefix) {
		if (string.IsNullOrWhiteSpace(prefixedId)) {
			throw new ArgumentException("ID cannot be null or empty", nameof(prefixedId));
		}

		var parts = prefixedId.Split(SEPARATOR, 2);
		if (parts.Length != 2) {
			throw new FormatException($"Invalid prefixed ID format: {prefixedId}");
		}

		var actualPrefix = parts[0];
		return actualPrefix != expectedPrefix
			? throw new ArgumentException(
				$"Invalid ID prefix. Expected '{expectedPrefix}{SEPARATOR}' but got '{actualPrefix}{SEPARATOR}'",
				nameof(prefixedId))
			: !Guid.TryParse(parts[1], out var guid)
				? throw new FormatException($"Invalid GUID in prefixed ID: {prefixedId}")
				: guid;
	}

	/// <summary>
	///     Try to parse prefixed ID string
	/// </summary>
	public static bool TryToGuid(string? prefixedId, out Guid id) {
		id = Guid.Empty;
		if (string.IsNullOrWhiteSpace(prefixedId)) {
			return false;
		}

		var parts = prefixedId.Split(SEPARATOR, 2);
		return parts.Length == 2 && Guid.TryParse(parts[1], out id);
	}

	/// <summary>
	///     Try to parse prefixed ID string with prefix validation
	/// </summary>
	public static bool TryToGuid(string? prefixedId, string expectedPrefix, out Guid id) {
		id = Guid.Empty;
		if (string.IsNullOrWhiteSpace(prefixedId)) {
			return false;
		}

		var parts = prefixedId.Split(SEPARATOR, 2);
		return parts.Length == 2 && parts[0] == expectedPrefix && Guid.TryParse(parts[1], out id);
	}

	/// <summary>
	///     Get the prefix from a prefixed ID
	/// </summary>
	public static string? GetPrefix(string? prefixedId) {
		if (string.IsNullOrWhiteSpace(prefixedId)) {
			return null;
		}

		var separatorIndex = prefixedId.IndexOf(SEPARATOR);
		return separatorIndex <= 0 ? null : prefixedId[..separatorIndex];
	}
}
