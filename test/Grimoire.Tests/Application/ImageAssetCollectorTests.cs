namespace Grimoire.Tests.Application;

using Grimoire.Application.Export;
using Grimoire.Domain.Common;
using Grimoire.Domain.Entity.Book;
using Grimoire.Domain.Entity.Book.Segment;
using Xunit;

public sealed class ImageAssetCollectorStaticTests {
	// ── BuildExportFileName ────────────────────────────────────────────────────

	[Fact]
	public void BuildExportFileName_BasicName_LowercasedWithIndexPadded() {
		var result = ImageAssetCollector.BuildExportFileName("Cover.jpg", 1);
		Assert.Equal("cover_001.jpg", result);
	}

	[Fact]
	public void BuildExportFileName_IndexPaddedToThreeDigits() {
		Assert.Equal("img_001.png", ImageAssetCollector.BuildExportFileName("img.png", 1));
		Assert.Equal("img_042.png", ImageAssetCollector.BuildExportFileName("img.png", 42));
		Assert.Equal("img_999.png", ImageAssetCollector.BuildExportFileName("img.png", 999));
	}

	[Fact]
	public void BuildExportFileName_NameWithSpaces_SpacesPreserved() {
		var result = ImageAssetCollector.BuildExportFileName("my cover image.png", 3);
		Assert.Equal("my cover image_003.png", result);
	}

	[Fact]
	public void BuildExportFileName_NameWithInvalidChars_Sanitized() {
		var result = ImageAssetCollector.BuildExportFileName("my\0cover.png", 3);
		Assert.Equal("my_cover_003.png", result);
	}

	[Fact]
	public void BuildExportFileName_PreservesExtension() {
		Assert.EndsWith(".webp", ImageAssetCollector.BuildExportFileName("anim.webp", 1));
		Assert.EndsWith(".gif", ImageAssetCollector.BuildExportFileName("anim.gif", 1));
		Assert.EndsWith(".jpeg", ImageAssetCollector.BuildExportFileName("photo.jpeg", 1));
	}

	// ── GenerateFileMap ────────────────────────────────────────────────────────

	[Fact]
	public void GenerateFileMap_EmptyAssets_ReturnsEmptyMap() {
		var result = ImageAssetCollector.GenerateFileMap(new Dictionary<string, ResolvedAsset>());
		Assert.Empty(result);
	}

	[Fact]
	public void GenerateFileMap_MultipleAssets_UniqueFilenamesWithIncrementingIndex() {
		var seriesId = Guid.CreateVersion7();
		var assetA = MakeAsset(seriesId, "cover.png");
		var assetB = MakeAsset(seriesId, "figure.jpg");
		var input = new Dictionary<string, ResolvedAsset> {
			["key-a"] = new ResolvedAsset(assetA, () => Task.FromResult<Stream?>(null)),
			["key-b"] = new ResolvedAsset(assetB, () => Task.FromResult<Stream?>(null))
		};

		var map = ImageAssetCollector.GenerateFileMap(input);

		Assert.Equal(2, map.Count);
		Assert.True(map.ContainsKey("key-a"));
		Assert.True(map.ContainsKey("key-b"));
		// ensure index values differ
		Assert.NotEqual(map["key-a"], map["key-b"]);
	}

	[Fact]
	public void GenerateFileMap_AssetKeys_MappedToSanitizedLowercaseFilenames() {
		var seriesId = Guid.CreateVersion7();
		var asset = MakeAsset(seriesId, "MyImage.PNG");
		var input = new Dictionary<string, ResolvedAsset> {
			["k"] = new ResolvedAsset(asset, () => Task.FromResult<Stream?>(null))
		};

		var map = ImageAssetCollector.GenerateFileMap(input);

		var name = map["k"];
		Assert.Equal("myimage_001.PNG", name); // lowercased filename part, extension casing preserved
	}

	// ── helpers ────────────────────────────────────────────────────────────────

	private static AssetModel MakeAsset(Guid seriesId, string fileName) => new() {
		Id = Guid.CreateVersion7(),
		SeriesId = seriesId,
		OwnerNodeId = seriesId,
		Path = fileName,
		OriginalFileName = fileName,
		FileHash = Guid.NewGuid().ToString("N"),
		RefType = AssetRefType.Content,
		ContentType = "image/png"
	};
}
