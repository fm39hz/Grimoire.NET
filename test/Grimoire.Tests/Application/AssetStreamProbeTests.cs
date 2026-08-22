namespace Grimoire.Tests.Application;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Export;
using Grimoire.Application.Service.Contract;
using Grimoire.Domain.Common;
using Grimoire.Domain.Entity.Book;
using Xunit;

/// <summary>
///     Verifies <see cref="AssetStreamProbe.IsReadableAsync"/> catches the empty-stream
///     and failure cases the previous inline StreamExistsAsync checks let through.
/// </summary>
public sealed class AssetStreamProbeTests {
	[Fact]
	public async Task NonNullNonEmptyStream_IsReadable() {
		var assetId = Guid.CreateVersion7();
		var storage = new StubStorage(assetId, static () => new AssetFileResult {
			Stream = Bytes("abcdefg"),
			ContentType = "image/jpeg",
			FileName = "cover.jpg"
		});

		Assert.True(await AssetStreamProbe.IsReadableAsync(storage, assetId));
	}

	[Fact]
	public async Task EmptyStream_IsNotReadable() {
		var assetId = Guid.CreateVersion7();
		var storage = new StubStorage(assetId, static () => new AssetFileResult {
			Stream = Bytes(""),
			ContentType = "image/jpeg",
			FileName = "cover.jpg"
		});

		Assert.False(await AssetStreamProbe.IsReadableAsync(storage, assetId));
	}

	[Fact]
	public async Task NullStreamResult_IsNotReadable() {
		var assetId = Guid.CreateVersion7();
		var storage = new StubStorage(assetId, static () => null);

		Assert.False(await AssetStreamProbe.IsReadableAsync(storage, assetId));
	}

	[Fact]
	public async Task StorageThrows_IsNotReadable() {
		var assetId = Guid.CreateVersion7();
		var storage = new StubStorage(assetId, static () => throw new IOException("blob disappeared"));

		Assert.False(await AssetStreamProbe.IsReadableAsync(storage, assetId));
	}

	private static Stream Bytes(string content) =>
		new MemoryStream(Encoding.UTF8.GetBytes(content));

	/// <summary>
	///     Minimal IStorageService stub: only GetFileStreamAsync is implemented, matching
	///     the surface AssetStreamProbe actually uses. The producer delegate lets a test
	///     inject a throwing producer or a null result without constructor ambiguity.
	/// </summary>
	private sealed class StubStorage : IStorageService {
		private readonly Func<AssetFileResult?> _produce;

		public StubStorage(Guid expectedAssetId, Func<AssetFileResult?> produce) {
			ExpectedAssetId = expectedAssetId;
			_produce = produce;
		}

		private Guid ExpectedAssetId { get; }

		public Task<AssetFileResult?> GetFileStreamAsync(Guid assetId, CancellationToken cancellationToken = default) {
			Assert.Equal(ExpectedAssetId, assetId);
			return Task.FromResult(_produce());
		}

		// Unused by the probe — throw to surface accidental coupling.
		public Task<AssetModel> UploadAssetAsync(Guid seriesId, Stream content, string contentType, string originalFileName,
			AssetRefType refType, string? prefix = null, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();

		public Task DeleteFileAsync(Guid assetId, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();
	}
}