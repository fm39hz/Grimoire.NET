namespace Grimoire.Application.Export;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Domain.Common;
using Service.Contract;

/// <summary>
///     Probes storage-backed asset streams for the export pipeline. Shared by
///     <see cref="CoverResolver"/> (package cover) and
///     <see cref="ImageAssetCollector"/> (inline + volume-title images).
/// </summary>
public static class AssetStreamProbe {
	/// <summary>
	///     Reports whether the backing stream for an asset exists and carries at
	///     least one byte. Guards against two silent-failure cases the previous
	///     inline <c>StreamExistsAsync</c> implementations missed:
	///     <list type="bullet">
	///         <item>The storage layer returns a non-null <see cref="AssetFileResult"/>
	///         whose stream is empty (0-byte upload, truncated object).</item>
	///         <item>Opening the stream throws (object deleted between id resolution
	///         and fetch).</item>
	///     </list>
	///     The probe opens, reads, and disposes one stream; callers re-fetch via
	///     <see cref="IStorageService.GetFileStreamAsync"/> when building the package,
	///     which stays safe for non-seekable (S3) streams.
	/// </summary>
	public static async Task<bool> IsReadableAsync(
		IStorageService storageService,
		Guid assetId,
		CancellationToken cancellationToken = default) {
		try {
			var result = await storageService.GetFileStreamAsync(assetId, cancellationToken);
			if (result == null) {
				return false;
			}

			await using (result.Stream) {
				// Reading a single byte rejects 0-byte streams while touching the backing
				// store only once. Magic-byte media-type detection is intentionally not
				// performed here (out of scope per the cover-validation decision).
				var buffer = new byte[1];
				return await result.Stream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken) > 0;
			}
		}
		catch {
			// Deleted between id resolution and fetch, signature mismatch, store error —
			// treat the asset as unavailable rather than letting a broken stream reach the zip.
			return false;
		}
	}
}