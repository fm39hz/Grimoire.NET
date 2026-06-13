import { describe, expect, test } from "bun:test";
import path from "node:path";

const repoRoot = path.resolve(import.meta.dir, "../..");

const read = async (relativePath: string) =>
	await Bun.file(path.join(repoRoot, relativePath)).text();

describe("unified book tree whitebox", () => {
	test("BookNode is the persisted hierarchy model and BookShelf stays logical", async () => {
		const nodeType = await read("src/Grimoire.Domain/Entity/Book/BookNodeType.cs");
		const nodeModel = await read("src/Grimoire.Domain/Entity/Book/BookNodeModel.cs");

		expect(nodeType).toContain("Series");
		expect(nodeType).toContain("Volume");
		expect(nodeType).toContain("Chapter");
		expect(nodeType).not.toContain("BookShelf");
		expect(nodeModel).toContain("Guid? ParentId");
		expect(nodeModel).toContain("double Order");
		expect(nodeModel).toContain("required string Title");
	});

	test("EF mapping of book_nodes table", async () => {
		const dbContext = await read("src/Grimoire.Infrastructure/Persistence/Database/ApplicationDbContext.cs");

		expect(dbContext).toContain("DbSet<BookNodeModel> BookNodes");
		expect(dbContext).toContain("new { n.ParentId, n.Order }).IsUnique()");
	});

	test("BookTreeService owns hierarchy invariants and legacy service facades delegate to it", async () => {
		const treeService = await read("src/Grimoire.Application/Service/Implementation/BookTreeService.cs");
		const seriesService = await read("src/Grimoire.Application/Service/Implementation/SeriesService.cs");
		const volumeService = await read("src/Grimoire.Application/Service/Implementation/VolumeService.cs");
		const chapterService = await read("src/Grimoire.Application/Service/Implementation/ChapterService.cs");

		expect(treeService).toContain("ValidateParent");
		expect(treeService).toContain("Series nodes must be root-level nodes");
		expect(treeService).toContain("Volume ? BookNodeType.Series : BookNodeType.Volume");
		expect(treeService).toContain("FindChildByOrder(newParentId, newOrder");
		expect(treeService).toContain("DeleteSubtree");
		expect(seriesService).toContain("seriesNodeService.CreateSeries");
		expect(seriesService).toContain("volumeNodeService.FindVolumes");
		expect(volumeService).toContain("volumeNodeService.CreateVolume");
		expect(volumeService).toContain("chapterNodeService.FindChapters");
		expect(chapterService).toContain("bookTreeService.CreateNode");
		expect(chapterService).toContain("bookTreeService.MoveNode");
	});

	test("export and freshness use the tree source of truth", async () => {
		const context = await read("src/Grimoire.Application/Export/BookExportContext.cs");
		const resolver = await read("src/Grimoire.Application/Export/VolumeResolver.cs");
		const freshness = await read("src/Grimoire.Infrastructure/Persistence/Repository/SeriesExportRecordRepository.cs");

		expect(context).toContain("BookTreeDto Tree");
		expect(resolver).toContain("bookTreeService.FindVolumes");
		expect(freshness.toLowerCase()).toContain("context.booknodes");
		expect(freshness.toLowerCase()).not.toContain("join v in context.volumes on c.volumeid equals v.id");
	});
});
