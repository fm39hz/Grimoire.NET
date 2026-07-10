namespace Grimoire.Domain.Common.ValueObject;

using System;
using System.Collections.Generic;
using System.Linq;

public readonly struct BookPath(string value) : IEquatable<BookPath> {
	private readonly string _value = value ?? string.Empty;

	public string Value => _value ?? string.Empty;

	public int Level {
		get {
			if (string.IsNullOrEmpty(_value)) {
				return 0;
			}

			return _value.Split('.').Length;
		}
	}

	public BookPath GetParent() {
		if (string.IsNullOrEmpty(_value)) {
			return new BookPath(string.Empty);
		}

		var parts = _value.Split('.');
		if (parts.Length <= 1) {
			return new BookPath(string.Empty);
		}

		return new BookPath(string.Join(".", parts.SkipLast(1)));
	}

	public bool IsDescendantOf(BookPath ancestor) {
		var p = Value;
		var a = ancestor.Value;
		if (string.IsNullOrEmpty(a)) {
			return false;
		}

		return p == a || p.StartsWith(a + ".");
	}

	public Guid GetSeriesId() {
		if (string.IsNullOrEmpty(_value)) {
			return Guid.Empty;
		}

		var parts = _value.Split('.');
		if (parts.Length > 0 && parts[0].Length > 1 && parts[0].StartsWith('n')) {
			if (Guid.TryParseExact(parts[0][1..], "N", out var guid)) {
				return guid;
			}
		}
		return Guid.Empty;
	}

	public Guid GetVolumeId() {
		if (string.IsNullOrEmpty(_value)) {
			return Guid.Empty;
		}

		var parts = _value.Split('.');
		if (parts.Length > 1 && parts[1].Length > 1 && parts[1].StartsWith('n')) {
			if (Guid.TryParseExact(parts[1][1..], "N", out var guid)) {
				return guid;
			}
		}
		return Guid.Empty;
	}

	public Guid GetLastNodeGuid() {
		if (string.IsNullOrEmpty(_value)) {
			return Guid.Empty;
		}

		var parts = _value.Split('.');
		var last = parts[^1];
		if (last.Length > 1 && last.StartsWith('n')) {
			if (Guid.TryParseExact(last[1..], "N", out var guid)) {
				return guid;
			}
		}
		return Guid.Empty;
	}

	public BookPath GetSubpath(int offset, int length) {
		if (string.IsNullOrEmpty(_value)) {
			return new BookPath(string.Empty);
		}

		var parts = _value.Split('.');
		return new BookPath(string.Join(".", parts.Skip(offset).Take(length)));
	}

	public static BookPath? FindLowestCommonAncestor(IEnumerable<BookPath> paths) {
		var list = paths.ToList();
		if (list.Count == 0) {
			return null;
		}

		var distinctPaths = list.Select(static p => p.Value).Distinct().ToList();
		if (distinctPaths.Count == 1) {
			return new BookPath(distinctPaths[0]);
		}

		var commonParts = distinctPaths[0].Split('.');
		foreach (var path in distinctPaths.Skip(1)) {
			var parts = path.Split('.');
			var tempCommon = new List<string>();
			for (var i = 0; i < Math.Min(commonParts.Length, parts.Length); i++) {
				if (commonParts[i] == parts[i]) {
					tempCommon.Add(commonParts[i]);
				}
				else {
					break;
				}
			}
			commonParts = [.. tempCommon];
			if (commonParts.Length == 0) {
				break;
			}
		}

		if (commonParts.Length > 0) {
			return new BookPath(string.Join(".", commonParts));
		}
		return null;
	}

	public override string ToString() => Value;
	public bool Equals(BookPath other) => Value == other.Value;
	public override bool Equals(object? obj) => obj is BookPath other && Equals(other);
	public override int GetHashCode() => Value.GetHashCode();

	public static bool operator ==(BookPath left, BookPath right) => left.Equals(right);
	public static bool operator !=(BookPath left, BookPath right) => !left.Equals(right);

	public static implicit operator string(BookPath path) => path.Value;
	public static implicit operator BookPath(string value) => new(value);
}
