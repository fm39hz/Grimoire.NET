namespace Grimoire.Domain.Common.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

public static class LTreeExtensions {
	/// <summary>
	///     Retrieves the series GUID from the LTree path representation (assuming format: n{seriesGuid}[.n{volumeGuid}...]).
	///     Safely parses client-side.
	/// </summary>
	public static Guid GetSeriesId(this LTree path) {
		var parts = path.ToString().Split('.');
		if (parts.Length > 0 && parts[0].Length > 1 && parts[0].StartsWith('n')) {
			if (Guid.TryParseExact(parts[0][1..], "N", out var guid)) return guid;
		}
		return Guid.Empty;
	}

	/// <summary>
	///     Retrieves the volume GUID from the LTree path representation (assuming format: n{seriesGuid}.n{volumeGuid}[.n{chapterGuid}...]).
	///     Safely parses client-side.
	/// </summary>
	public static Guid GetVolumeId(this LTree path) {
		var parts = path.ToString().Split('.');
		if (parts.Length > 1 && parts[1].Length > 1 && parts[1].StartsWith('n')) {
			if (Guid.TryParseExact(parts[1][1..], "N", out var guid)) return guid;
		}
		return Guid.Empty;
	}

	/// <summary>
	///     Gets the parent path of the current LTree path.
	///     Safely executes client-side to prevent EF translation exceptions in tests.
	/// </summary>
	public static LTree GetParent(this LTree path) {
		var parts = path.ToString().Split('.');
		if (parts.Length <= 1) return string.Empty;
		return string.Join(".", parts.SkipLast(1));
	}

	/// <summary>
	///     In-memory implementation of IsDescendantOf for client-side evaluation (e.g. unit tests, in-memory repos).
	///     Prevents Npgsql's LTree.IsDescendantOf throw.
	/// </summary>
	public static bool IsDescendantOfClient(this LTree path, LTree ancestor) {
		string p = path.ToString();
		string a = ancestor.ToString();
		return p == a || p.StartsWith(a + ".");
	}

	/// <summary>
	///     Safely extracts the length (number of labels) of the path on the client-side.
	///     Prevents Npgsql's LTree.NLevel throw.
	/// </summary>
	public static int GetNLevelClient(this LTree path) {
		string p = path.ToString();
		if (string.IsNullOrEmpty(p)) return 0;
		return p.Split('.').Length;
	}

	/// <summary>
	///     Safely extracts a subpath on the client-side.
	///     Prevents Npgsql's LTree.Subpath throw.
	/// </summary>
	public static LTree GetSubpathClient(this LTree path, int offset, int length) {
		var parts = path.ToString().Split('.');
		return string.Join(".", parts.Skip(offset).Take(length));
	}

	/// <summary>
	///     Safely resolves the Lowest Common Ancestor on the client side from a collection of paths.
	///     Prevents Npgsql's LTree.LongestCommonAncestor throw.
	/// </summary>
	public static LTree? FindLowestCommonAncestorClient(this IEnumerable<LTree> paths) {
		var list = paths.ToList();
		if (list.Count == 0) return null;
		
		var distinctPaths = list.Select(p => p.ToString()).Distinct().ToList();
		if (distinctPaths.Count == 1) return distinctPaths[0];

		var commonParts = distinctPaths[0].Split('.');
		foreach (var path in distinctPaths.Skip(1)) {
			var parts = path.Split('.');
			var tempCommon = new List<string>();
			for (int i = 0; i < Math.Min(commonParts.Length, parts.Length); i++) {
				if (commonParts[i] == parts[i]) {
					tempCommon.Add(commonParts[i]);
				} else {
					break;
				}
			}
			commonParts = tempCommon.ToArray();
			if (commonParts.Length == 0) break;
		}
		if (commonParts.Length > 0) {
			return new LTree(string.Join(".", commonParts));
		}
		return null;
	}

	/// <summary>
	///     Resolves owner GUID from the last segment of the LTree path on the client side.
	/// </summary>
	public static Guid GetLastNodeGuid(this LTree path) {
		string p = path.ToString();
		if (string.IsNullOrEmpty(p)) return Guid.Empty;
		var parts = p.Split('.');
		var last = parts[^1];
		if (last.Length > 1 && last.StartsWith('n')) {
			if (Guid.TryParseExact(last[1..], "N", out var guid)) return guid;
		}
		return Guid.Empty;
	}
}
